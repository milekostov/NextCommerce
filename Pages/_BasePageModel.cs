using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace NextCommerce.Pages
{
    public class BasePageModel : PageModel
    {
        protected readonly IConfiguration _configuration;

        public BasePageModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void CheckAdminStatus()
        {
            var userId = HttpContext.Session.GetInt32("LoggedUser");
            if (userId.HasValue)
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();
                    var command = new SqlCommand(
                        "SELECT IsAdmin FROM Users WHERE Id = @UserId",
                        connection);
                    command.Parameters.AddWithValue("@UserId", userId.Value);
                    var result = command.ExecuteScalar();
                    ViewData["IsAdmin"] = result != null && (bool)result;
                }
            }
        }
    }
} 