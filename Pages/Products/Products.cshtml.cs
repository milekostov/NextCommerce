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

        public ProductsModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            LoadProducts();
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
    }
} 