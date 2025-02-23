using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace NextCommerce.Pages.Cart
{
    public class ViewCartModel : BasePageModel
    {
        public ViewCartModel(IConfiguration configuration)
            : base(configuration)
        {
        }

        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        public decimal CartTotal => CartItems.Sum(item => item.Price * item.Quantity);

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

        public IActionResult OnPostUpdateQuantity(int productId, int quantity)
        {
            var userId = HttpContext.Session.GetInt32("LoggedUser");
            if (!userId.HasValue)
            {
                return RedirectToPage("/Login");
            }

            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var command = new SqlCommand(
                    "UPDATE Cart SET Quantity = @Quantity WHERE UserId = @UserId AND ProductId = @ProductId",
                    connection);
                command.Parameters.AddWithValue("@UserId", userId.Value);
                command.Parameters.AddWithValue("@ProductId", productId);
                command.Parameters.AddWithValue("@Quantity", quantity);
                command.ExecuteNonQuery();
            }

            return RedirectToPage();
        }

        public IActionResult OnPostRemoveItem(int productId)
        {
            var userId = HttpContext.Session.GetInt32("LoggedUser");
            if (!userId.HasValue)
            {
                return RedirectToPage("/Login");
            }

            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var command = new SqlCommand(
                    "DELETE FROM Cart WHERE UserId = @UserId AND ProductId = @ProductId",
                    connection);
                command.Parameters.AddWithValue("@UserId", userId.Value);
                command.Parameters.AddWithValue("@ProductId", productId);
                command.ExecuteNonQuery();
            }

            return RedirectToPage();
        }

        private void LoadCartItems(int userId)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var command = new SqlCommand(
                    @"SELECT c.ProductId, p.Name, p.Price, c.Quantity, p.Quantity as AvailableQuantity, p.Image 
                    FROM Cart c 
                    JOIN Products p ON c.ProductId = p.Id 
                    WHERE c.UserId = @UserId",
                    connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using (var reader = command.ExecuteReader())
                {
                    CartItems.Clear();
                    while (reader.Read())
                    {
                        CartItems.Add(new CartItem
                        {
                            ProductId = reader.GetInt32(0),
                            ProductName = reader.GetString(1),
                            Price = reader.GetDecimal(2),
                            Quantity = reader.GetInt32(3),
                            AvailableQuantity = reader.GetInt32(4),
                            Image = reader.IsDBNull(5) ? null : reader.GetString(5)
                        });
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
            public string Image { get; set; }
        }
    }
}