using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.IO;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace NextCommerce.Pages.Products
{
    public class AddProductModel : PageModel
    {
        private readonly IConfiguration _configuration;

        [BindProperty]
        public NewProductModel NewProduct { get; set; } = new NewProductModel();

        [BindProperty]
        public IFormFile ImageUpload { get; set; }

        public List<Category> Categories { get; set; } = new List<Category>();

        public AddProductModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            LoadCategories();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                LoadCategories();
                return Page();
            }

            string imagePath = null;

            if (ImageUpload != null && ImageUpload.Length > 0)
            {
                // Generate a random file name
                var randomFileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageUpload.FileName);
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ImageUploads");
                var filePath = Path.Combine(uploadsFolder, randomFileName);

                // Ensure the directory exists
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Save the image
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    ImageUpload.CopyTo(stream);
                }

                imagePath = "/ImageUploads/" + randomFileName; // Store the relative path
            }

            // Save product details to the database
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var command = new SqlCommand(
                    "INSERT INTO Products (Name, Description, Price, CategoryId, Image, Quantity) VALUES (@Name, @Description, @Price, @CategoryId, @Image, @Quantity)", 
                    connection);
                command.Parameters.AddWithValue("@Name", NewProduct.Name);
                command.Parameters.AddWithValue("@Description", NewProduct.Description);
                command.Parameters.AddWithValue("@Price", NewProduct.Price);
                command.Parameters.AddWithValue("@CategoryId", NewProduct.CategoryId);
                command.Parameters.AddWithValue("@Image", imagePath);
                command.Parameters.AddWithValue("@Quantity", NewProduct.Quantity);
                command.ExecuteNonQuery();
            }

            return RedirectToPage("/Products/Products");
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

        public class NewProductModel
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public decimal Price { get; set; }
            public int CategoryId { get; set; }
            public int Quantity { get; set; }
        }

        public class Category
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
