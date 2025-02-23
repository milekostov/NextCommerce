using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace NextCommerce.Pages.Admin
{
    public class OrderManagementModel : BasePageModel
    {
        private readonly IConfiguration _configuration;
        public List<OrderInfo> Orders { get; set; } = new List<OrderInfo>();

        public OrderManagementModel(IConfiguration configuration)
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

            // Check if user is admin (you might want to add an IsAdmin column to Users table)
            if (!IsAdmin(userId.Value))
            {
                return RedirectToPage("/Index");
            }

            LoadOrders();
            return Page();
        }

        public IActionResult OnPostUpdateStatus(int orderId, string status)
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

            return RedirectToPage();
        }

        private void LoadOrders()
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var command = new SqlCommand(
                    @"SELECT o.Id, o.OrderDate, o.TotalAmount, o.Status, o.ShippingAddress, u.Username 
                      FROM Orders o 
                      JOIN Users u ON o.UserId = u.Id 
                      ORDER BY o.OrderDate DESC",
                    connection);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Orders.Add(new OrderInfo
                        {
                            Id = reader.GetInt32(0),
                            OrderDate = reader.GetDateTime(1),
                            TotalAmount = reader.GetDecimal(2),
                            Status = reader.GetString(3),
                            ShippingAddress = reader.GetString(4),
                            Username = reader.GetString(5)
                        });
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

        public class OrderInfo
        {
            public int Id { get; set; }
            public DateTime OrderDate { get; set; }
            public decimal TotalAmount { get; set; }
            public string Status { get; set; }
            public string ShippingAddress { get; set; }
            public string Username { get; set; }
        }
    }
} 