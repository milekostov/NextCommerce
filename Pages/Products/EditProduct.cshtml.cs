using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace NextCommerce.Pages.Products
{
    public class EditProductModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public EditProductModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public ProductModel Product { get; set; } = new ProductModel();
        public List<Category> Categories { get; set; } = new List<Category>();
        public string ErrorMessage { get; set; }

        public IActionResult OnGet(int id)
        {
            LoadCategories();
            LoadProduct(id);
            
            if (Product.Id == 0)
            {
                return RedirectToPage("/Products/Products");
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                LoadCategories();
                return Page();
            }

            UpdateProduct();
            return RedirectToPage("/Products/Products");
        }

        private void LoadProduct(int id)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var command = new SqlCommand(
                    "SELECT Id, Name, Description, Price, CategoryId, Image " +
                    "FROM Products WHERE Id = @Id", connection);
                command.Parameters.AddWithValue("@Id", id);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        Product = new ProductModel
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Description = reader.GetString(2),
                            Price = reader.GetDecimal(3),
                            CategoryId = reader.GetInt32(4),
                            Image = !reader.IsDBNull(5) ? reader.GetString(5) : null
                        };
                    }
                }
            }
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

        private void UpdateProduct()
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var command = new SqlCommand(
                    "UPDATE Products SET Name = @Name, Description = @Description, " +
                    "Price = @Price, CategoryId = @CategoryId WHERE Id = @Id", connection);

                command.Parameters.AddWithValue("@Id", Product.Id);
                command.Parameters.AddWithValue("@Name", Product.Name);
                command.Parameters.AddWithValue("@Description", Product.Description);
                command.Parameters.AddWithValue("@Price", Product.Price);
                command.Parameters.AddWithValue("@CategoryId", Product.CategoryId);

                command.ExecuteNonQuery();
            }
        }

        public class ProductModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public decimal Price { get; set; }
            public int CategoryId { get; set; }
            public string Image { get; set; }
        }

        public class Category
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
} 