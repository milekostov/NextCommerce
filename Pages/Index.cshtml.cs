using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace NextCommerce.Pages;

public class IndexModel : BasePageModel
{
    public IndexModel(IConfiguration configuration)
        : base(configuration)
    {
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

    public string? Username { get; set; }
}
