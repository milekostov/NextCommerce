using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace NextCommerce.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IConfiguration _configuration;

    public string Username { get; set; }

    public IndexModel(ILogger<IndexModel> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public void OnGet()
    {
        var userId = HttpContext.Session.GetInt32("LoggedUser");
        if (userId.HasValue)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var command = new SqlCommand("SELECT Username FROM Users WHERE Id = @UserId", connection);
                command.Parameters.AddWithValue("@UserId", userId.Value);
                
                var result = command.ExecuteScalar();
                if (result != null)
                {
                    Username = result.ToString();
                }
            }
        }
    }
}
