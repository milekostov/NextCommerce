using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace NextCommerce.Pages.Stores
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public IndexModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<Store> Stores { get; set; } = new List<Store>();

        public void OnGet()
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var command = new SqlCommand("SELECT StoreId, StoreName, Location FROM Stores", connection);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Stores.Add(new Store
                        {
                            Id = reader.GetInt32(0),
                            StoreName = reader.GetString(1),
                            Location = reader.GetString(2)
                        });
                    }
                }
            }
        }

        public class Store
        {
            public int Id { get; set; }
            public string StoreName { get; set; }
            public string Location { get; set; }
        }
    }
}
