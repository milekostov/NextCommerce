using NextCommerceShop.Models;

namespace NextCommerceShop.ViewModels
{
    public class AdminDashboardViewModel
    {
        // KPI cards
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int OrdersToday { get; set; }
        public decimal TodaysRevenue { get; set; }

        // Tables / lists
        public List<Order> LatestOrders { get; set; } = new();

        public List<LowStockProductDto> LowStockProducts { get; set; } = new();
        public List<BestSellingProductDto> BestSellingProducts { get; set; } = new();
        public List<RecentCustomerDto> RecentCustomers { get; set; } = new();
    }

    public class LowStockProductDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public int Stock { get; set; }
    }

    public class BestSellingProductDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public int TotalSold { get; set; }
    }

    public class RecentCustomerDto
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public DateTime LastOrderDate { get; set; }
        public int OrdersCount { get; set; }
    }
}
