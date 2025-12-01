using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NextCommerceShop.Data;
using NextCommerceShop.Models;

namespace NextCommerceShop.Controllers
{
    [Authorize] // only logged-in users can see their orders
    public class MyOrdersController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MyOrdersController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /MyOrders
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        // GET: /MyOrders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);

            var order = await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.UserId == userId && o.Id == id)
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return NotFound(); // or Forbid() if you want stricter behavior
            }

            return View(order);
        }
    }
}
