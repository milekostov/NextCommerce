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
                    @"SELECT p.Id, p.Name, p.Description, p.Price, p.Quantity, p.Image, 
                             p.DateCreated, c.Name as CategoryName 
                      FROM Products p
                      LEFT JOIN Category c ON p.CategoryId = c.Id
                      WHERE p.IsDeleted = 0", connection);  // Only show non-deleted products

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
                            Image = reader.IsDBNull(5) ? null : reader.GetString(5),
                            DateCreated = reader.GetDateTime(6),
                            CategoryName = reader.IsDBNull(7) ? null : reader.GetString(7)
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
                                "SELECT Quantity FROM Products WHERE Id = @ProductId",
                                connection, transaction);
                            checkCommand.Parameters.AddWithValue("@ProductId", productId);
                            var availableQuantity = (int)checkCommand.ExecuteScalar();

                            if (quantity > availableQuantity)
                            {
                                TempData["ErrorMessage"] = "Requested quantity not available.";
                                return RedirectToPage();
                            }

                            // Check if item already exists in cart
                            var cartCommand = new SqlCommand(
                                "SELECT Quantity FROM Cart WHERE UserId = @UserId AND ProductId = @ProductId",
                                connection, transaction);
                            cartCommand.Parameters.AddWithValue("@UserId", userId.Value);
                            cartCommand.Parameters.AddWithValue("@ProductId", productId);
                            var existingQuantity = cartCommand.ExecuteScalar();

                            if (existingQuantity != null)
                            {
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

                            // Update product quantity
                            var updateProductCommand = new SqlCommand(
                                "UPDATE Products SET Quantity = Quantity - @Quantity WHERE Id = @ProductId",
                                connection, transaction);
                            updateProductCommand.Parameters.AddWithValue("@ProductId", productId);
                            updateProductCommand.Parameters.AddWithValue("@Quantity", quantity);
                            updateProductCommand.ExecuteNonQuery();

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
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error adding item to cart: " + ex.Message;
                return RedirectToPage();
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