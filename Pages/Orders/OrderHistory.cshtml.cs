using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace NextCommerce.Pages.Orders
{
    public class OrderHistoryModel : BasePageModel
    {
        private readonly IConfiguration _configuration;
        public List<OrderWithItems> Orders { get; set; } = new List<OrderWithItems>();

        public OrderHistoryModel(IConfiguration configuration)
            : base(configuration)
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
                    @"SELECT Id, OrderNumber, OrderDate, TotalAmount, Status, PaymentStatus, ShippingAddress 
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
                            OrderNumber = reader.GetString(1),
                            OrderDate = reader.GetDateTime(2),
                            TotalAmount = reader.GetDecimal(3),
                            Status = reader.GetString(4),
                            PaymentStatus = reader.GetString(5),
                            ShippingAddress = reader.GetString(6),
                            Items = new List<OrderItemInfo>()
                        });
                    }
                }

                foreach (var order in Orders)
                {
                    var itemsCommand = new SqlCommand(
                        @"SELECT ProductId, ProductName, CategoryId, CategoryName, Quantity, PriceAtTime 
                          FROM OrderItems 
                          WHERE OrderId = @OrderId",
                        connection);
                    itemsCommand.Parameters.AddWithValue("@OrderId", order.Id);

                    using (var reader = itemsCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            order.Items.Add(new OrderItemInfo
                            {
                                ProductId = reader.GetInt32(0),
                                ProductName = reader.GetString(1),
                                CategoryId = reader.GetInt32(2),
                                CategoryName = reader.GetString(3),
                                Quantity = reader.GetInt32(4),
                                Price = reader.GetDecimal(5)
                            });
                        }
                    }
                }
            }
        }

        public class OrderWithItems
        {
            public int Id { get; set; }
            public string OrderNumber { get; set; }
            public DateTime OrderDate { get; set; }
            public decimal TotalAmount { get; set; }
            public string Status { get; set; }
            public string PaymentStatus { get; set; }
            public string ShippingAddress { get; set; }
            public List<OrderItemInfo> Items { get; set; }
        }

        public class OrderItemInfo
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public int CategoryId { get; set; }
            public string CategoryName { get; set; }
            public int Quantity { get; set; }
            public decimal Price { get; set; }
        }
    }
} 