using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace NextCommerce.Pages.Admin
{
    public class OrderDetailsModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public OrderWithItems? Order { get; set; }

        public OrderDetailsModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult OnGet(int orderId)
        {
            var userId = HttpContext.Session.GetInt32("LoggedUser");
            if (!userId.HasValue)
            {
                return RedirectToPage("/Login");
            }

            // Check if user is admin
            if (!IsAdmin(userId.Value))
            {
                return RedirectToPage("/Index");
            }

            LoadOrder(orderId);
            return Page();
        }

        public IActionResult OnPost(int orderId, string status)
        {
            var userId = HttpContext.Session.GetInt32("LoggedUser");
            if (!userId.HasValue || !IsAdmin(userId.Value))
            {
                return RedirectToPage("/Index");
            }

            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var command = new SqlCommand(
                    "UPDATE Orders SET Status = @Status WHERE Id = @OrderId",
                    connection);
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@OrderId", orderId);
                command.ExecuteNonQuery();
            }

            return RedirectToPage(new { orderId });
        }

        private void LoadOrder(int orderId)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                
                // Load order details
                var command = new SqlCommand(
                    @"SELECT o.Id, o.OrderNumber, o.OrderDate, o.TotalAmount, o.Status, 
                             o.PaymentStatus, o.ShippingAddress, u.Username
                      FROM Orders o
                      JOIN Users u ON o.UserId = u.Id
                      WHERE o.Id = @OrderId",
                    connection);
                command.Parameters.AddWithValue("@OrderId", orderId);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        Order = new OrderWithItems
                        {
                            Id = reader.GetInt32(0),
                            OrderNumber = reader.GetString(1),
                            OrderDate = reader.GetDateTime(2),
                            TotalAmount = reader.GetDecimal(3),
                            Status = reader.GetString(4),
                            PaymentStatus = reader.GetString(5),
                            ShippingAddress = reader.GetString(6),
                            Username = reader.GetString(7),
                            Items = new List<OrderItemInfo>()
                        };
                    }
                }

                if (Order != null)
                {
                    // Load order items
                    command = new SqlCommand(
                        @"SELECT ProductId, ProductName, CategoryName, Quantity, PriceAtTime
                          FROM OrderItems
                          WHERE OrderId = @OrderId",
                        connection);
                    command.Parameters.AddWithValue("@OrderId", orderId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Order.Items.Add(new OrderItemInfo
                            {
                                ProductId = reader.GetInt32(0),
                                ProductName = reader.GetString(1),
                                CategoryName = reader.GetString(2),
                                Quantity = reader.GetInt32(3),
                                Price = reader.GetDecimal(4)
                            });
                        }
                    }
                }
            }
        }

        private bool IsAdmin(int userId)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var command = new SqlCommand(
                    "SELECT IsAdmin FROM Users WHERE Id = @UserId",
                    connection);
                command.Parameters.AddWithValue("@UserId", userId);
                var result = command.ExecuteScalar();
                return result != null && (bool)result;
            }
        }

        public class OrderWithItems
        {
            public int Id { get; set; }
            public string OrderNumber { get; set; } = "";
            public DateTime OrderDate { get; set; }
            public decimal TotalAmount { get; set; }
            public string Status { get; set; } = "";
            public string PaymentStatus { get; set; } = "";
            public string ShippingAddress { get; set; } = "";
            public string Username { get; set; } = "";
            public List<OrderItemInfo> Items { get; set; } = new List<OrderItemInfo>();
        }

        public class OrderItemInfo
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; } = "";
            public string CategoryName { get; set; } = "";
            public int Quantity { get; set; }
            public decimal Price { get; set; }
        }
    }
} 