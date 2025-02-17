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
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                Message = "Username and password are required";
                return Page();
            }

            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand(
                        "SELECT PasswordHash, PasswordSalt FROM Users WHERE Username = @Username", 
                        connection);
                    
                    command.Parameters.AddWithValue("@Username", Username);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string storedHash = reader.GetString(0);
                            string storedSalt = reader.GetString(1);

                            // Convert stored salt back to bytes
                            byte[] salt = Convert.FromBase64String(storedSalt);

                            // Create hash from provided password
                            byte[] hashBytes;
                            using (var aes = Aes.Create())
                            {
                                aes.KeySize = 256;
                                var pbkdf2 = new Rfc2898DeriveBytes(Password, salt, 10000, HashAlgorithmName.SHA256);
                                hashBytes = pbkdf2.GetBytes(32);
                            }

                            string calculatedHash = Convert.ToBase64String(hashBytes);

                            if (calculatedHash == storedHash)
                            {
                                // Password is correct - set session
                                HttpContext.Session.SetString("LoggedUser", Username);
                                return RedirectToPage("/Index");
                            }
                        }
                    }
                }

                Message = "Invalid username or password";
                return Page();
            }
            catch (Exception)
            {
                Message = "An error occurred during login";
                return Page();
            }
        }
    }
} 