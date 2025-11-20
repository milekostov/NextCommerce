using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace NextCommerce.Pages.Stores
{
    public class AddStoreModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public AddStoreModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public string StoreName { get; set; }

        [BindProperty]
        public string Location { get; set; }

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public void OnGet() { }

        public void OnPost()
        {
            if (string.IsNullOrWhiteSpace(StoreName) || string.IsNullOrWhiteSpace(Location))
            {
                ErrorMessage = "Please provide both store name and location.";
                return;
            }

            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();
                    var command = new SqlCommand("INSERT INTO Stores (StoreName, Location) VALUES (@Name, @Location)", connection);
                    command.Parameters.AddWithValue("@Name", StoreName);
                    command.Parameters.AddWithValue("@Location", Location);
                    command.ExecuteNonQuery();

                    SuccessMessage = "Store added successfully!";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error adding store: " + ex.Message;
            }
        }
    }
}
