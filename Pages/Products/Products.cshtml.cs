using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System;
using System.IO;

namespace NextCommerce.Pages.Products
{
    public class ProductsModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public List<Product> ProductList { get; set; } = new List<Product>();
        public List<Category> Categories { get; set; } = new List<Category>(); // List to hold categories

        [BindProperty]
        public Product NewProduct { get; set; } = new Product(); // Property to bind new product data

        public ProductsModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            LoadProducts();
            LoadCategories(); // Load categories on GET
        }

        public void LoadProducts()
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var command = new SqlCommand(
                    "SELECT p.Id, p.Name, p.Description, p.Price, p.DateCreated, c.Name AS CategoryName, p.Image, p.Quantity " +
                    "FROM Products p LEFT JOIN Category c ON p.CategoryId = c.Id", connection);
                using (var reader = command.ExecuteReader())
                {
                    ProductList.Clear();
                    while (reader.Read())
                    {
                        ProductList.Add(new Product
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Description = reader.GetString(2),
                            Price = reader.GetDecimal(3),
                            DateCreated = reader.GetDateTime(4),
                            CategoryName = reader.IsDBNull(5) ? null : reader.GetString(5), // Get category name
                            Image = reader.IsDBNull(6) ? null : reader.GetString(6), // Get image path
                            Quantity = reader.GetInt32(7)  // Add this line
                        });
                    }
                }
            }
        }

        public void LoadCategories() // Method to load categories
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var command = new SqlCommand("SELECT Id, Name FROM Category", connection);
                using (var reader = command.ExecuteReader())
                {
                    Categories.Clear();
                    while (reader.Read())
                    {
                        Categories.Add(new Category
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1)
                        });
                    }
                }
            }
        }

        public IActionResult OnPostDelete(int id)
        {
            string imagePath = null;

            // Retrieve the image path from the database
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var command = new SqlCommand("SELECT Image FROM Products WHERE Id = @Id", connection);
                command.Parameters.AddWithValue("@Id", id);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        imagePath = reader.IsDBNull(0) ? null : reader.GetString(0); // Get the image path
                    }
                }
            }

            // Delete the product from the database
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var command = new SqlCommand("DELETE FROM Products WHERE Id = @Id", connection);
                command.Parameters.AddWithValue("@Id", id);
                command.ExecuteNonQuery();
            }

            // Delete the image file from the server
            if (!string.IsNullOrEmpty(imagePath))
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imagePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath); // Delete the file
                }
            }

            return RedirectToPage();
        }

        public IActionResult OnPostEdit(int id, string name, string description, decimal price, int categoryId)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var command = new SqlCommand("UPDATE Products SET Name = @Name, Description = @Description, Price = @Price, CategoryId = @CategoryId WHERE Id = @Id", connection);
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@Description", description);
                command.Parameters.AddWithValue("@Price", price);
                command.Parameters.AddWithValue("@CategoryId", categoryId);
                command.ExecuteNonQuery();
            }

            return RedirectToPage();
        }

        public IActionResult OnPostAddToCart(int productId, int quantity = 1)
        {
            var userId = HttpContext.Session.GetInt32("LoggedUser");
            if (!userId.HasValue)
            {
                return RedirectToPage("/Login");
            }

            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // First, check if the product exists and get its available quantity
                        int availableQuantity;
                        using (var command = new SqlCommand(
                            "SELECT Quantity FROM Products WHERE Id = @ProductId",
                            connection, transaction))
                        {
                            command.Parameters.AddWithValue("@ProductId", productId);
                            var result = command.ExecuteScalar();
                            if (result == null)
                            {
                                TempData["ErrorMessage"] = "Product not found.";
                                return RedirectToPage();
                            }
                            availableQuantity = Convert.ToInt32(result);
                        }

                        // Check current quantity in cart
                        int currentCartQuantity = 0;
                        using (var command = new SqlCommand(
                            "SELECT Quantity FROM Cart WHERE UserId = @UserId AND ProductId = @ProductId",
                            connection, transaction))
                        {
                            command.Parameters.AddWithValue("@UserId", userId.Value);
                            command.Parameters.AddWithValue("@ProductId", productId);
                            var result = command.ExecuteScalar();
                            if (result != null)
                            {
                                currentCartQuantity = Convert.ToInt32(result);
                            }
                        }

                        // Calculate total requested quantity
                        int totalRequestedQuantity = currentCartQuantity + quantity;

                        // Validate against available quantity
                        if (totalRequestedQuantity > availableQuantity)
                        {
                            TempData["ErrorMessage"] = $"Cannot add {quantity} items. Only {availableQuantity - currentCartQuantity} items available.";
                            return RedirectToPage();
                        }

                        // If item exists in cart, update quantity
                        if (currentCartQuantity > 0)
                        {
                            using (var command = new SqlCommand(
                                "UPDATE Cart SET Quantity = @Quantity WHERE UserId = @UserId AND ProductId = @ProductId",
                                connection, transaction))
                            {
                                command.Parameters.AddWithValue("@UserId", userId.Value);
                                command.Parameters.AddWithValue("@ProductId", productId);
                                command.Parameters.AddWithValue("@Quantity", totalRequestedQuantity);
                                command.ExecuteNonQuery();
                            }
                        }
                        else // If item doesn't exist in cart, insert new record
                        {
                            using (var command = new SqlCommand(
                                "INSERT INTO Cart (UserId, ProductId, Quantity) VALUES (@UserId, @ProductId, @Quantity)",
                                connection, transaction))
                            {
                                command.Parameters.AddWithValue("@UserId", userId.Value);
                                command.Parameters.AddWithValue("@ProductId", productId);
                                command.Parameters.AddWithValue("@Quantity", quantity);
                                command.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        TempData["SuccessMessage"] = "Item added to cart successfully.";
                        return RedirectToPage();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        TempData["ErrorMessage"] = "Error adding item to cart: " + ex.Message;
                        return RedirectToPage();
                    }
                }
            }
        }

        // Nested Product class
        public class Product
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public decimal Price { get; set; }
            public DateTime DateCreated { get; set; }
            public int? CategoryId { get; set; }
            public string CategoryName { get; set; } // New property for category name
            public string Image { get; set; } // Add this line to include the Image property
            public int Quantity { get; set; }  // Add this line
        }

        // Nested Category class
        public class Category
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class NewProductModel
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public decimal Price { get; set; }
            public int CategoryId { get; set; } // Ensure this property exists
        }
    }
} 