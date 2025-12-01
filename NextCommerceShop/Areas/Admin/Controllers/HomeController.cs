using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NextCommerceShop.Data;
using NextCommerceShop.Models;
using NextCommerceShop.ViewModels;   // ⬅️ IMPORTANT

namespace NextCommerceShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Admin or /Admin/Home
        public async Task<IActionResult> Index()
        {
            var todayUtc = DateTime.UtcNow.Date;
            var tomorrowUtc = todayUtc.AddDays(1);

            // === KPI CARDS ===
            var totalOrders = await _context.Orders.CountAsync();

            var pendingOrders = await _context.Orders
                .CountAsync(o => o.Status == OrderStatus.Pending);

            var processingOrders = await _context.Orders
                .CountAsync(o => o.Status == OrderStatus.Processing);

            var totalRevenue = await _context.Orders
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var ordersToday = await _context.Orders
                .CountAsync(o => o.CreatedAt >= todayUtc && o.CreatedAt < tomorrowUtc);

            var todaysRevenue = await _context.Orders
                .Where(o => o.CreatedAt >= todayUtc &&
                            o.CreatedAt < tomorrowUtc &&
                            o.Status != OrderStatus.Cancelled &&
                            o.Status != OrderStatus.Refunded)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            // === LATEST ORDERS ===
            var latestOrders = await _context.Orders
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToListAsync();

            // === LOW STOCK PRODUCTS ===
            const int stockThreshold = 5; // adjust as you like

            var lowStockProducts = await _context.Products
                .Where(p => p.StockQuantity <= stockThreshold)
                .OrderBy(p => p.StockQuantity)
                .Take(5)
                .Select(p => new LowStockProductDto
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    Stock = p.StockQuantity
                })
                .ToListAsync();

            // === BEST-SELLING PRODUCTS ===
            var bestSellingProducts = await _context.OrderItems
                .GroupBy(oi => new { oi.ProductId, oi.ProductName })
                .Select(g => new BestSellingProductDto
                {
                    ProductId = g.Key.ProductId,
                    Name = g.Key.ProductName,
                    TotalSold = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .ToListAsync();

            // === RECENT CUSTOMERS ===
            var recentCustomers = await _context.Orders
                .GroupBy(o => new { o.FullName, o.Email })
                .Select(g => new RecentCustomerDto
                {
                    Name = g.Key.FullName,
                    Email = g.Key.Email,
                    LastOrderDate = g.Max(x => x.CreatedAt),
                    OrdersCount = g.Count()
                })
                .OrderByDescending(x => x.LastOrderDate)
                .Take(5)
                .ToListAsync();

            // === BUILD VIEWMODEL ===
            var vm = new AdminDashboardViewModel
            {
                TotalOrders = totalOrders,
                PendingOrders = pendingOrders,
                ProcessingOrders = processingOrders,
                TotalRevenue = totalRevenue,
                OrdersToday = ordersToday,
                TodaysRevenue = todaysRevenue,
                LatestOrders = latestOrders,
                LowStockProducts = lowStockProducts,
                BestSellingProducts = bestSellingProducts,
                RecentCustomers = recentCustomers
            };

            return View(vm);
        }
    }
}
