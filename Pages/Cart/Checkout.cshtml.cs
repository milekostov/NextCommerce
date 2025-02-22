using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NextCommerce.Pages.Cart
{
    public class CheckoutModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public CheckoutModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public string ShippingAddress { get; set; }
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        public decimal CartTotal => CartItems.Sum(item => item.Price * item.Quantity);
        public string ErrorMessage { get; set; }

        public class CartItem
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public decimal Price { get; set; }
            public int Quantity { get; set; }
            public int AvailableQuantity { get; set; }
        }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("LoggedUser");
            if (!userId.HasValue)
            {
                return RedirectToPage("/Login");
            }

            LoadCartItems(userId.Value);
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
                        // Create order
                        var orderCommand = new SqlCommand(
                            @"INSERT INTO Orders (UserId, OrderDate, TotalAmount, Status, ShippingAddress) 
                              VALUES (@UserId, @OrderDate, @TotalAmount, @Status, @ShippingAddress);
                              SELECT SCOPE_IDENTITY();", connection, transaction);

                        orderCommand.Parameters.AddWithValue("@UserId", userId.Value);
                        orderCommand.Parameters.AddWithValue("@OrderDate", DateTime.Now);
                        orderCommand.Parameters.AddWithValue("@TotalAmount", CartTotal);
                        orderCommand.Parameters.AddWithValue("@Status", "Pending");
                        orderCommand.Parameters.AddWithValue("@ShippingAddress", ShippingAddress);

                        var orderId = Convert.ToInt32(orderCommand.ExecuteScalar());

                        // Add order items and update product quantities
                        foreach (var item in CartItems)
                        {
                            // Add order item
                            var orderItemCommand = new SqlCommand(
                                @"INSERT INTO OrderItems (OrderId, ProductId, Quantity, PriceAtTime) 
                                  VALUES (@OrderId, @ProductId, @Quantity, @Price)", connection, transaction);

                            orderItemCommand.Parameters.AddWithValue("@OrderId", orderId);
                            orderItemCommand.Parameters.AddWithValue("@ProductId", item.ProductId);
                            orderItemCommand.Parameters.AddWithValue("@Quantity", item.Quantity);
                            orderItemCommand.Parameters.AddWithValue("@Price", item.Price);
                            orderItemCommand.ExecuteNonQuery();

                            // Update product quantity
                            var updateProductCommand = new SqlCommand(
                                @"UPDATE Products 
                                  SET Quantity = Quantity - @Quantity 
                                  WHERE Id = @ProductId", connection, transaction);

                            updateProductCommand.Parameters.AddWithValue("@ProductId", item.ProductId);
                            updateProductCommand.Parameters.AddWithValue("@Quantity", item.Quantity);
                            updateProductCommand.ExecuteNonQuery();
                        }

                        // Clear cart
                        var clearCartCommand = new SqlCommand(
                            "DELETE FROM Cart WHERE UserId = @UserId", connection, transaction);
                        clearCartCommand.Parameters.AddWithValue("@UserId", userId.Value);
                        clearCartCommand.ExecuteNonQuery();

                        transaction.Commit();
                        return RedirectToPage("/Orders/OrderHistory");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        ErrorMessage = "An error occurred while processing your order. Please try again.";
                        return Page();
                    }
                }
            }
        }

        private void LoadCartItems(int userId)
        {
            // ... (same as ViewCart.cshtml.cs LoadCartItems method)
        }
    }
} 