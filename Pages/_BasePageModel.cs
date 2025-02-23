using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NextCommerce.Pages
{
    public class BasePageModel : PageModel
    {
        protected readonly IConfiguration _configuration;
        public bool IsAdmin { get; private set; }

        public BasePageModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            base.OnPageHandlerExecuting(context);
            CheckAdminStatus();
        }

        private void CheckAdminStatus()
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
                    IsAdmin = result != null && (bool)result;
                    ViewData["IsAdmin"] = IsAdmin;
                }
            }
        }
    }
} 