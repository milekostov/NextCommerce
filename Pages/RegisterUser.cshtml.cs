using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace NextCommerce.Pages
{
    public class RegisterUserModel : PageModel
    {
        private readonly IConfiguration _configuration;
        [BindProperty]
        public string Username { get; set; } = string.Empty;
        [BindProperty]
        public string Password { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public RegisterUserModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Username and password are required";
                return Page();
            }

            try
            {
                // Generate a random salt
                byte[] salt = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(salt);
                }

                // Create the hash using the password and salt
                byte[] hashBytes;
                using (var aes = Aes.Create())
                {
                    aes.KeySize = 256;
                    var pbkdf2 = new Rfc2898DeriveBytes(Password, salt, 10000, HashAlgorithmName.SHA256);
                    hashBytes = pbkdf2.GetBytes(32);
                }

                // Convert hash and salt to base64 strings for storage
                string hashString = Convert.ToBase64String(hashBytes);
                string saltString = Convert.ToBase64String(salt);

                // Save to database
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand(
                        "INSERT INTO Users (Username, PasswordHash, PasswordSalt, DateCreated) " +
                        "VALUES (@Username, @PasswordHash, @PasswordSalt, GETDATE())", connection);
                    
                    command.Parameters.AddWithValue("@Username", Username);
                    command.Parameters.AddWithValue("@PasswordHash", hashString);
                    command.Parameters.AddWithValue("@PasswordSalt", saltString);
                    
                    command.ExecuteNonQuery();
                }

                return RedirectToPage("Index");
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627)  // Unique constraint error
                {
                    ErrorMessage = "Username already exists";
                }
                else
                {
                    ErrorMessage = "Database error occurred";
                }
                return Page();
            }
            catch (Exception)
            {
                ErrorMessage = "An error occurred during registration";
                return Page();
            }
        }
    }
} 