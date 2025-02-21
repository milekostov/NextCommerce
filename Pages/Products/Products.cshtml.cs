using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System;

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
                    "SELECT p.Id, p.Name, p.Description, p.Price, p.DateCreated, p.CategoryId " +
                    "FROM Products p", connection);
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
                            CategoryId = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5)
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

        public IActionResult OnPostAddProduct() // Method to handle adding a new product
        {
            if (!ModelState.IsValid)
            {
                LoadCategories(); // Reload categories if model state is invalid
                return Page();
            }

            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var command = new SqlCommand("INSERT INTO Products (Name, Description, Price, CategoryId) VALUES (@Name, @Description, @Price, @CategoryId)", connection);
                command.Parameters.AddWithValue("@Name", NewProduct.Name);
                command.Parameters.AddWithValue("@Description", NewProduct.Description);
                command.Parameters.AddWithValue("@Price", NewProduct.Price);
                command.Parameters.AddWithValue("@CategoryId", NewProduct.CategoryId);
                command.ExecuteNonQuery();
            }

            return RedirectToPage(); // Redirect to refresh the product list
        }

        public IActionResult OnPostDelete(int id)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var command = new SqlCommand("DELETE FROM Products WHERE Id = @Id", connection);
                command.Parameters.AddWithValue("@Id", id);
                command.ExecuteNonQuery();
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
            public DateTime DateCreated { get; set; }
            public int? CategoryId { get; set; }
        }

        // Nested Category class
        public class Category
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
} 