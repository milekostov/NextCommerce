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
                        // Debug log
                        Console.WriteLine("Starting order creation...");

                        // Create the order
                        int orderId;
                        using (var command = new SqlCommand(
                            @"INSERT INTO Orders (UserId, OrderDate, TotalAmount, Status, ShippingAddress) 
                              VALUES (@UserId, @OrderDate, @TotalAmount, @Status, @ShippingAddress);
                              SELECT SCOPE_IDENTITY();", connection, transaction))
                        {
                            command.Parameters.AddWithValue("@UserId", userId.Value);
                            command.Parameters.AddWithValue("@OrderDate", DateTime.Now);
                            command.Parameters.AddWithValue("@TotalAmount", CartTotal);
                            command.Parameters.AddWithValue("@Status", "Pending");
                            command.Parameters.AddWithValue("@ShippingAddress", ShippingAddress);

                            orderId = Convert.ToInt32(command.ExecuteScalar());
                            Console.WriteLine($"Created order with ID: {orderId}"); // Debug log
                        }

                        // Add order items
                        foreach (var item in CartItems)
                        {
                            using (var command = new SqlCommand(
                                @"INSERT INTO OrderItems (OrderId, ProductId, CategoryId, ProductName, CategoryName, Quantity, PriceAtTime) 
                                  VALUES (@OrderId, @ProductId, @CategoryId, @ProductName, @CategoryName, @Quantity, @Price)", 
                                connection, transaction))
                            {
                                // Generate unique order number (e.g., ORD-20240217-001)
                                var orderNumber = $"ORD-{DateTime.Now:yyyyMMdd}-{orderId:D3}";

                                // Update Orders table with order number
                                using (var updateOrderCommand = new SqlCommand(
                                    "UPDATE Orders SET OrderNumber = @OrderNumber WHERE Id = @OrderId",
                                    connection, transaction))
                                {
                                    updateOrderCommand.Parameters.AddWithValue("@OrderNumber", orderNumber);
                                    updateOrderCommand.Parameters.AddWithValue("@OrderId", orderId);
                                    updateOrderCommand.ExecuteNonQuery();
                                }

                                // Get category information
                                int categoryId;
                                string categoryName;
                                using (var categoryCommand = new SqlCommand(
                                    @"SELECT c.Id, c.Name 
                                      FROM Categories c 
                                      JOIN Products p ON p.CategoryId = c.Id 
                                      WHERE p.Id = @ProductId",
                                    connection, transaction))
                                {
                                    categoryCommand.Parameters.AddWithValue("@ProductId", item.ProductId);
                                    using (var reader = categoryCommand.ExecuteReader())
                                    {
                                        reader.Read();
                                        categoryId = reader.GetInt32(0);
                                        categoryName = reader.GetString(1);
                                    }
                                }

                                command.Parameters.AddWithValue("@OrderId", orderId);
                                command.Parameters.AddWithValue("@ProductId", item.ProductId);
                                command.Parameters.AddWithValue("@CategoryId", categoryId);
                                command.Parameters.AddWithValue("@ProductName", item.ProductName);
                                command.Parameters.AddWithValue("@CategoryName", categoryName);
                                command.Parameters.AddWithValue("@Quantity", item.Quantity);
                                command.Parameters.AddWithValue("@Price", item.Price);
                                command.ExecuteNonQuery();
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
                        return RedirectToPage("/Orders/OrderHistory");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error during checkout: {ex.Message}"); // Debug log
                        transaction.Rollback();
                        ErrorMessage = "An error occurred while processing your order. Please try again.";
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