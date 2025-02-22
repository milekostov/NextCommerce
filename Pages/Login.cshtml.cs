using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;

namespace NextCommerce.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IConfiguration _configuration;
        
        [BindProperty]
        public string Username { get; set; } = string.Empty;
        
        [BindProperty]
        public string Password { get; set; } = string.Empty;
        
        public string Message { get; set; } = string.Empty;

        public LoginModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            // Clear any existing session
            HttpContext.Session.Clear();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var command = new SqlCommand("SELECT Id, PasswordHash, PasswordSalt FROM Users WHERE Username = @Username", connection);
                command.Parameters.AddWithValue("@Username", Username);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var storedHash = reader.GetString(1);
                        var storedSalt = reader.GetString(2);
                        var userId = reader.GetInt32(0); // Get the user's ID

                        // Verify the password
                        var passwordHash = HashPassword(Password, storedSalt);
                        if (passwordHash == storedHash)
                        {
                            // Store the user's ID instead of username
                            HttpContext.Session.SetInt32("LoggedUser", userId);
                            return RedirectToPage("/Index");
                        }
                    }
                }
            }

            ModelState.AddModelError("", "Invalid username or password");
            return Page();
        }

        private string HashPassword(string password, string storedSalt)
        {
            // Convert stored salt back to bytes
            byte[] salt = Convert.FromBase64String(storedSalt);

            // Create hash from provided password
            byte[] hashBytes;
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
                hashBytes = pbkdf2.GetBytes(32);
            }

            return Convert.ToBase64String(hashBytes);
        }
    }
} 