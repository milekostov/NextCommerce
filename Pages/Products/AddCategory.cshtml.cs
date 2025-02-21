using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace NextCommerce.Pages.Products
{
    public class AddCategoryModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public AddCategoryModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public NewCategoryModel NewCategory { get; set; } = new NewCategoryModel();

        public List<Category> Categories { get; set; } = new List<Category>();

        public IActionResult OnGet()
        {
            LoadCategories();
            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                LoadCategories();
                return Page();
            }

            InsertCategory(NewCategory);
            return RedirectToPage("/Products/AddCategory");
        }

        public IActionResult OnPostDelete(int id)
        {
            DeleteCategory(id);
            return RedirectToPage("/Products/AddCategory");
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

        private void InsertCategory(NewCategoryModel newCategory)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var command = new SqlCommand("INSERT INTO Category (Name) VALUES (@Name)", connection);
                command.Parameters.AddWithValue("@Name", newCategory.Name);
                command.ExecuteNonQuery();
            }
        }

        private void DeleteCategory(int id)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var command = new SqlCommand("DELETE FROM Category WHERE Id = @Id", connection);
                command.Parameters.AddWithValue("@Id", id);
                command.ExecuteNonQuery();
            }
        }

        public class NewCategoryModel
        {
            public string Name { get; set; }
        }

        public class Category
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
} 