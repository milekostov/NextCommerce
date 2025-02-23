using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System;
using System.IO;

namespace NextCommerce.Pages.Products
{
    public class ProductsModel : BasePageModel
    {
        private readonly IConfiguration _configuration;

        public List<Product> ProductList { get; set; } = new List<Product>();
        public List<Category> Categories { get; set; } = new List<Category>(); // List to hold categories

        [BindProperty]
        public Product NewProduct { get; set; } = new Product(); // Property to bind new product data

        public ProductsModel(IConfiguration configuration)
            : base(configuration)
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
                    @"SELECT p.Id, p.Name, p.Description, p.Price, p.Quantity, p.DateCreated, 
                             p.Image, c.Name as CategoryName, c.Id as CategoryId,
                             ISNULL(AVG(CAST(pr.Rating as FLOAT)), 0) as AverageRating,
                             COUNT(pr.Id) as ReviewCount
                      FROM Products p
                      LEFT JOIN Category c ON p.CategoryId = c.Id
                      LEFT JOIN ProductReviews pr ON p.Id = pr.ProductId
                      WHERE p.IsDeleted = 0
                      GROUP BY p.Id, p.Name, p.Description, p.Price, p.Quantity, p.DateCreated, 
                               p.Image, c.Name, c.Id",
                    connection);

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
                            Quantity = reader.GetInt32(4),
                            DateCreated = reader.GetDateTime(5),
                            Image = reader.IsDBNull(6) ? null : reader.GetString(6),
                            CategoryName = reader.IsDBNull(7) ? null : reader.GetString(7),
                            CategoryId = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                            AverageRating = reader.GetDouble(9),
                            ReviewCount = reader.GetInt32(10)
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

        public IActionResult OnPostDelete(int productId)
        {
            var userId = HttpContext.Session.GetInt32("LoggedUser");
            if (!userId.HasValue || !IsAdmin)
            {
                return RedirectToPage("/Login");
            }

            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();
                    var command = new SqlCommand(
                        "UPDATE Products SET IsDeleted = 1 WHERE Id = @ProductId",
                        connection);
                    command.Parameters.AddWithValue("@ProductId", productId);
                    command.ExecuteNonQuery();

                    TempData["SuccessMessage"] = "Product deleted successfully.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting product: " + ex.Message;
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

        public IActionResult OnPostAddToCart(int productId, int quantity)
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
                            // Check product availability
                            var checkCommand = new SqlCommand(
                                "SELECT Quantity FROM Products WHERE Id = @ProductId AND IsDeleted = 0",
                                connection, transaction);
                            checkCommand.Parameters.AddWithValue("@ProductId", productId);
                            var availableQuantity = (int)checkCommand.ExecuteScalar();

                            if (quantity > availableQuantity)
                            {
                                TempData["ErrorMessage"] = "Requested quantity not available.";
                                return RedirectToPage();
                            }

                            // Check existing cart quantity
                            var cartCommand = new SqlCommand(
                                "SELECT Quantity FROM Cart WHERE UserId = @UserId AND ProductId = @ProductId",
                                connection, transaction);
                            cartCommand.Parameters.AddWithValue("@UserId", userId.Value);
                            cartCommand.Parameters.AddWithValue("@ProductId", productId);
                            var existingQuantity = cartCommand.ExecuteScalar();

                            if (existingQuantity != null)
                            {
                                // Check if total quantity would exceed available stock
                                if ((int)existingQuantity + quantity > availableQuantity)
                                {
                                    TempData["ErrorMessage"] = "Cannot add more items. Would exceed available stock.";
                                    return RedirectToPage();
                                }

                                // Update existing cart item
                                var updateCommand = new SqlCommand(
                                    "UPDATE Cart SET Quantity = Quantity + @Quantity WHERE UserId = @UserId AND ProductId = @ProductId",
                                    connection, transaction);
                                updateCommand.Parameters.AddWithValue("@UserId", userId.Value);
                                updateCommand.Parameters.AddWithValue("@ProductId", productId);
                                updateCommand.Parameters.AddWithValue("@Quantity", quantity);
                                updateCommand.ExecuteNonQuery();
                            }
                            else
                            {
                                // Insert new cart item
                                var insertCommand = new SqlCommand(
                                    "INSERT INTO Cart (UserId, ProductId, Quantity) VALUES (@UserId, @ProductId, @Quantity)",
                                    connection, transaction);
                                insertCommand.Parameters.AddWithValue("@UserId", userId.Value);
                                insertCommand.Parameters.AddWithValue("@ProductId", productId);
                                insertCommand.Parameters.AddWithValue("@Quantity", quantity);
                                insertCommand.ExecuteNonQuery();
                            }

                            transaction.Commit();
                            TempData["SuccessMessage"] = "Item added to cart successfully.";
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            TempData["ErrorMessage"] = "Error adding item to cart: " + ex.Message;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error adding item to cart: " + ex.Message;
            }

            return RedirectToPage();
        }

        // Nested Product class
        public class Product
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public decimal Price { get; set; }
            public int Quantity { get; set; }
            public DateTime DateCreated { get; set; }
            public string? Image { get; set; }
            public string CategoryName { get; set; }
            public int CategoryId { get; set; }
            public double AverageRating { get; set; }
            public int ReviewCount { get; set; }
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