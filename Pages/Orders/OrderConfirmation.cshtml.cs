using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace NextCommerce.Pages.Orders
{
    public class OrderConfirmationModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public OrderConfirmationModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public OrderInfo Order { get; set; }
        public List<OrderItemInfo> OrderItems { get; set; } = new List<OrderItemInfo>();

        public IActionResult OnGet(int orderId)
        {
            var userId = HttpContext.Session.GetInt32("LoggedUser");
            if (!userId.HasValue)
            {
                return RedirectToPage("/Login");
            }

            LoadOrder(orderId, userId.Value);
            return Page();
        }

        private void LoadOrder(int orderId, int userId)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                
                // Load order details
                var orderCommand = new SqlCommand(
                    @"SELECT Id, OrderDate, TotalAmount, Status, ShippingAddress 
                      FROM Orders 
                      WHERE Id = @OrderId AND UserId = @UserId",
                    connection);
                orderCommand.Parameters.AddWithValue("@OrderId", orderId);
                orderCommand.Parameters.AddWithValue("@UserId", userId);

                using (var reader = orderCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        Order = new OrderInfo
                        {
                            Id = reader.GetInt32(0),
                            OrderDate = reader.GetDateTime(1),
                            TotalAmount = reader.GetDecimal(2),
                            Status = reader.GetString(3),
                            ShippingAddress = reader.GetString(4)
                        };
                    }
                }

                // Load order items
                var itemsCommand = new SqlCommand(
                    @"SELECT p.Name, oi.Quantity, oi.PriceAtTime 
                      FROM OrderItems oi
                      JOIN Products p ON oi.ProductId = p.Id
                      WHERE oi.OrderId = @OrderId",
                    connection);
                itemsCommand.Parameters.AddWithValue("@OrderId", orderId);

                using (var reader = itemsCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        OrderItems.Add(new OrderItemInfo
                        {
                            ProductName = reader.GetString(0),
                            Quantity = reader.GetInt32(1),
                            Price = reader.GetDecimal(2)
                        });
                    }
                }
            }
        }

        public class OrderInfo
        {
            public int Id { get; set; }
            public DateTime OrderDate { get; set; }
            public decimal TotalAmount { get; set; }
            public string Status { get; set; }
            public string ShippingAddress { get; set; }
        }

        public class OrderItemInfo
        {
            public string ProductName { get; set; }
            public int Quantity { get; set; }
            public decimal Price { get; set; }
        }
    }
} 