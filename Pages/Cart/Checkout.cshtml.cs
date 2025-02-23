using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NextCommerce.Pages.Cart
{
    public class CheckoutModel : BasePageModel
    {
        public CheckoutModel(IConfiguration configuration) : base(configuration)
        {
        }

        [BindProperty]
        public string ShippingAddress { get; set; }
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        public decimal CartTotal => CartItems.Sum(item => item.Price * item.Quantity);
        public string ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("LoggedUser");
            if (!userId.HasValue)
            {
                return RedirectToPage("/Login");
            }

            LoadCartItems(userId.Value);
            
            // Debug information
            Console.WriteLine($"Cart Items Count: {CartItems.Count}");
            foreach (var item in CartItems)
            {
                Console.WriteLine($"Product: {item.ProductName}, Quantity: {item.Quantity}, Price: {item.Price}");
            }
            
            return Page();
        }

        public IActionResult OnPost()
        {
            var userId = HttpContext.Session.GetInt32("LoggedUser");
            if (!userId.HasValue)
            {
                return RedirectToPage("/Login");
            }

            LoadCartItems(userId.Value);
            
            Console.WriteLine($"OnPost - Cart Items Count: {CartItems.Count}"); // Debug log

            if (CartItems.Count == 0)
            {
                ErrorMessage = "Your cart is empty";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(ShippingAddress))
            {
                ErrorMessage = "Shipping address is required";
                return Page();
            }

            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        Console.WriteLine("Starting order creation..."); // Debug log

                        // Create the order
                        int orderId;
                        var orderNumber = $"ORD-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000):D3}";
                        
                        // Debug log the SQL and values
                        Console.WriteLine($"Order Number: {orderNumber}");
                        Console.WriteLine($"Total Amount: {CartTotal}");
                        Console.WriteLine($"Shipping Address: {ShippingAddress}");

                        using (var command = new SqlCommand(
                            @"INSERT INTO Orders (UserId, OrderNumber, OrderDate, TotalAmount, Status, PaymentStatus, ShippingAddress) 
                              VALUES (@UserId, @OrderNumber, @OrderDate, @TotalAmount, @Status, @PaymentStatus, @ShippingAddress);
                              SELECT SCOPE_IDENTITY();", connection, transaction))
                        {
                            command.Parameters.AddWithValue("@UserId", userId.Value);
                            command.Parameters.AddWithValue("@OrderNumber", orderNumber);
                            command.Parameters.AddWithValue("@OrderDate", DateTime.Now);
                            command.Parameters.AddWithValue("@TotalAmount", CartTotal);
                            command.Parameters.AddWithValue("@Status", "Pending");
                            command.Parameters.AddWithValue("@PaymentStatus", "Pending");
                            command.Parameters.AddWithValue("@ShippingAddress", ShippingAddress);

                            orderId = Convert.ToInt32(command.ExecuteScalar());
                            Console.WriteLine($"Created order with ID: {orderId}"); // Debug log
                        }

                        // Add order items
                        foreach (var item in CartItems)
                        {
                            Console.WriteLine($"Processing item: {item.ProductName}"); // Debug log

                            // Get category information with fallback to "Uncategorized"
                            int categoryId = 1; // Default to first category
                            string categoryName = "Uncategorized";
                            using (var categoryCommand = new SqlCommand(
                                @"SELECT ISNULL(c.Id, 1), ISNULL(c.Name, 'Uncategorized')
                                  FROM Products p
                                  LEFT JOIN Category c ON p.CategoryId = c.Id
                                  WHERE p.Id = @ProductId",
                                connection, transaction))
                            {
                                categoryCommand.Parameters.AddWithValue("@ProductId", item.ProductId);
                                using (var reader = categoryCommand.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        categoryId = reader.GetInt32(0);
                                        categoryName = reader.GetString(1);
                                        Console.WriteLine($"Found category: {categoryName} (ID: {categoryId})"); // Debug log
                                    }
                                }
                            }

                            using (var command = new SqlCommand(
                                @"INSERT INTO OrderItems (OrderId, ProductId, CategoryId, ProductName, CategoryName, Quantity, PriceAtTime) 
                                  VALUES (@OrderId, @ProductId, @CategoryId, @ProductName, @CategoryName, @Quantity, @Price)", 
                                connection, transaction))
                            {
                                command.Parameters.AddWithValue("@OrderId", orderId);
                                command.Parameters.AddWithValue("@ProductId", item.ProductId);
                                command.Parameters.AddWithValue("@CategoryId", categoryId);
                                command.Parameters.AddWithValue("@ProductName", item.ProductName);
                                command.Parameters.AddWithValue("@CategoryName", categoryName);
                                command.Parameters.AddWithValue("@Quantity", item.Quantity);
                                command.Parameters.AddWithValue("@Price", item.Price);
                                command.ExecuteNonQuery();
                                Console.WriteLine($"Added order item for {item.ProductName}"); // Debug log
                            }

                            // Update product quantity
                            using (var command = new SqlCommand(
                                @"UPDATE Products 
                                  SET Quantity = Quantity - @Quantity 
                                  WHERE Id = @ProductId", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@ProductId", item.ProductId);
                                command.Parameters.AddWithValue("@Quantity", item.Quantity);
                                command.ExecuteNonQuery();
                                Console.WriteLine($"Updated quantity for product {item.ProductId}"); // Debug log
                            }
                        }

                        // Clear the cart
                        using (var command = new SqlCommand(
                            "DELETE FROM Cart WHERE UserId = @UserId", connection, transaction))
                        {
                            command.Parameters.AddWithValue("@UserId", userId.Value);
                            command.ExecuteNonQuery();
                            Console.WriteLine("Cart cleared"); // Debug log
                        }

                        transaction.Commit();
                        Console.WriteLine("Transaction committed successfully"); // Debug log
                        return RedirectToPage("/Orders/OrderConfirmation", new { orderId = orderId });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Error during checkout: {ex.Message}"); // Debug log
                        Console.WriteLine($"Stack trace: {ex.StackTrace}"); // Debug log
                        ErrorMessage = $"An error occurred while processing your order: {ex.Message}";
                        return Page();
                    }
                }
            }
        }

        private void LoadCartItems(int userId)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var command = new SqlCommand(
                    @"SELECT c.ProductId, p.Name, p.Price, c.Quantity, p.Quantity as AvailableQuantity 
                      FROM Cart c 
                      JOIN Products p ON c.ProductId = p.Id 
                      WHERE c.UserId = @UserId", connection);
                command.Parameters.AddWithValue("@UserId", userId);

                Console.WriteLine($"Loading cart items for user {userId}"); // Debug log

                using (var reader = command.ExecuteReader())
                {
                    CartItems.Clear();
                    while (reader.Read())
                    {
                        var item = new CartItem
                        {
                            ProductId = reader.GetInt32(0),
                            ProductName = reader.GetString(1),
                            Price = reader.GetDecimal(2),
                            Quantity = reader.GetInt32(3),
                            AvailableQuantity = reader.GetInt32(4)
                        };
                        CartItems.Add(item);
                        Console.WriteLine($"Added item: {item.ProductName}"); // Debug log
                    }
                }
            }
        }

        public class CartItem
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public decimal Price { get; set; }
            public int Quantity { get; set; }
            public int AvailableQuantity { get; set; }
        }
    }
} 