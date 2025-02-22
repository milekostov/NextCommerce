using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace NextCommerce.Pages.Orders
{
    public class OrderHistoryModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public List<OrderWithItems> Orders { get; set; } = new List<OrderWithItems>();

        public OrderHistoryModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("LoggedUser");
            if (!userId.HasValue)
            {
                return RedirectToPage("/Login");
            }

            LoadOrders(userId.Value);
            return Page();
        }

        private void LoadOrders(int userId)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var command = new SqlCommand(
                    @"SELECT Id, OrderDate, TotalAmount, Status, ShippingAddress 
                      FROM Orders 
                      WHERE UserId = @UserId 
                      ORDER BY OrderDate DESC",
                    connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Orders.Add(new OrderWithItems
                        {
                            Id = reader.GetInt32(0),
                            OrderDate = reader.GetDateTime(1),
                            TotalAmount = reader.GetDecimal(2),
                            Status = reader.GetString(3),
                            ShippingAddress = reader.GetString(4),
                            Items = new List<OrderItemInfo>()
                        });
                    }
                }

                // Load items for each order
                foreach (var order in Orders)
                {
                    var itemsCommand = new SqlCommand(
                        @"SELECT p.Name, oi.Quantity, oi.PriceAtTime 
                          FROM OrderItems oi
                          JOIN Products p ON oi.ProductId = p.Id
                          WHERE oi.OrderId = @OrderId",
                        connection);
                    itemsCommand.Parameters.AddWithValue("@OrderId", order.Id);

                    using (var reader = itemsCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            order.Items.Add(new OrderItemInfo
                            {
                                ProductName = reader.GetString(0),
                                Quantity = reader.GetInt32(1),
                                Price = reader.GetDecimal(2)
                            });
                        }
                    }
                }
            }
        }

        public class OrderWithItems
        {
            public int Id { get; set; }
            public DateTime OrderDate { get; set; }
            public decimal TotalAmount { get; set; }
            public string Status { get; set; }
            public string ShippingAddress { get; set; }
            public List<OrderItemInfo> Items { get; set; }
        }

        public class OrderItemInfo
        {
            public string ProductName { get; set; }
            public int Quantity { get; set; }
            public decimal Price { get; set; }
        }
    }
} 