using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NextCommerce.Pages.Cart
{
    public class ViewCartModel : BasePageModel
    {
        public ViewCartModel(IConfiguration configuration)
            : base(configuration)
        {
            CartItems = new List<CartItem>();
        }

        public List<CartItem> CartItems { get; set; }
        public decimal CartTotal => CartItems.Sum(item => item.Price * item.Quantity);

        public void OnGet()
        {
            var userId = HttpContext.Session.GetInt32("LoggedUser");
            if (userId.HasValue)
            {
                LoadCartItems(userId.Value);
            }
        }

        public IActionResult OnPostUpdateQuantity(int productId, int quantity)
        {
            var userId = HttpContext.Session.GetInt32("LoggedUser");
            if (!userId.HasValue)
            {
                return RedirectToPage("/Login");
            }

            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Check available quantity
                            var checkCommand = new SqlCommand(
                                "SELECT Quantity FROM Products WHERE Id = @ProductId AND IsDeleted = 0",
                                connection, transaction);
                            checkCommand.Parameters.AddWithValue("@ProductId", productId);
                            var availableQuantity = (int)checkCommand.ExecuteScalar();

                            if (quantity > availableQuantity)
                            {
                                TempData["ErrorMessage"] = $"Only {availableQuantity} items available.";
                                return RedirectToPage();
                            }

                            var updateCommand = new SqlCommand(
                                "UPDATE Cart SET Quantity = @Quantity WHERE UserId = @UserId AND ProductId = @ProductId",
                                connection, transaction);
                            updateCommand.Parameters.AddWithValue("@UserId", userId.Value);
                            updateCommand.Parameters.AddWithValue("@ProductId", productId);
                            updateCommand.Parameters.AddWithValue("@Quantity", quantity);
                            updateCommand.ExecuteNonQuery();

                            transaction.Commit();
                            TempData["SuccessMessage"] = "Cart updated successfully.";
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            TempData["ErrorMessage"] = "Error updating cart: " + ex.Message;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating cart: " + ex.Message;
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
                    @"SELECT c.ProductId, p.Name, p.Price, c.Quantity, p.Quantity as AvailableQuantity, p.Image,
                             CASE WHEN c.Quantity > p.Quantity THEN 1 ELSE 0 END as IsOverstock
                    FROM Cart c 
                    JOIN Products p ON c.ProductId = p.Id 
                    WHERE c.UserId = @UserId AND p.IsDeleted = 0",
                    connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using (var reader = command.ExecuteReader())
                {
                    CartItems.Clear();
                    while (reader.Read())
                    {
                        var cartQuantity = reader.GetInt32(3);
                        var availableQuantity = reader.GetInt32(4);
                        var isOverstock = reader.GetInt32(6) == 1;

                        CartItems.Add(new CartItem
                        {
                            ProductId = reader.GetInt32(0),
                            ProductName = reader.GetString(1),
                            Price = reader.GetDecimal(2),
                            Quantity = cartQuantity,
                            AvailableQuantity = availableQuantity,
                            Image = reader.IsDBNull(5) ? null : reader.GetString(5),
                            IsOutOfStock = availableQuantity == 0,
                            IsOverstock = isOverstock,
                            MaxAvailableQuantity = Math.Min(cartQuantity, availableQuantity)
                        });
                    }
                }

                // If any items are out of stock or overstock, show warning
                if (CartItems.Any(i => i.IsOutOfStock || i.IsOverstock))
                {
                    TempData["WarningMessage"] = "Some items in your cart are no longer available in the requested quantity.";
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
            public bool IsOutOfStock { get; set; }
            public bool IsOverstock { get; set; }
            public int MaxAvailableQuantity { get; set; }
        }
    }
}