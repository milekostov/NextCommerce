using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace NextCommerce.Pages.Products
{
    public class AddProductModel : PageModel
    {
        private readonly IConfiguration _configuration;

        [BindProperty]
        public Product NewProduct { get; set; } = new Product();

        public List<Category> Categories { get; set; } = new List<Category>();

        public AddProductModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            LoadCategories();
        }

        private void LoadCategories()
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

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                LoadCategories();
                return Page();
            }

            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var command = new SqlCommand(
                    "INSERT INTO Products (Name, Description, Price, CategoryId, DateCreated) VALUES (@Name, @Description, @Price, @CategoryId, GETDATE())", 
                    connection);
                
                command.Parameters.AddWithValue("@Name", NewProduct.Name);
                command.Parameters.AddWithValue("@Description", (object)NewProduct.Description ?? DBNull.Value);
                command.Parameters.AddWithValue("@Price", NewProduct.Price);
                command.Parameters.AddWithValue("@CategoryId", (object)NewProduct.CategoryId ?? DBNull.Value);

                command.ExecuteNonQuery();
            }

            return RedirectToPage("/Products/Products");
        }

        public class Product
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public decimal Price { get; set; }
            public int? CategoryId { get; set; }
        }

        public class Category
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
