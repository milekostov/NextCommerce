using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NextCommerceShop.Data;
using NextCommerceShop.Helpers;
using NextCommerceShop.Models;


namespace NextCommerceShop.Controllers
{
    public class ShopController : Controller
    {
        private readonly AppDbContext _context;

        public ShopController(AppDbContext context)
        {
            _context = context;
        }

        // Public catalog page
        public async Task<IActionResult> Index(int? categoryId)
        {
            var productsQuery = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            var products = await productsQuery.ToListAsync();

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.SelectedCategoryId = categoryId;

            return View(products);
        }

        // Product details page
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
        [HttpPost]
        public async Task<IActionResult> AddToCart(int id)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            const string cartKey = "Cart";

            // Get current cart from session or create new one
            var cart = HttpContext.Session.GetObject<List<CartItem>>(cartKey)
                       ?? new List<CartItem>();

            // See if product already in cart
            var existingItem = cart.FirstOrDefault(c => c.ProductId == product.Id);

            if (existingItem == null)
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    Quantity = 1
                });
            }
            else
            {
                existingItem.Quantity += 1;
            }

            // Save cart back to session
            HttpContext.Session.SetObject(cartKey, cart);

            // Go to cart page
            return RedirectToAction("Index", "Cart");
        }


    }
}
