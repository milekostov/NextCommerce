using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NextCommerceShop.Data;
using NextCommerceShop.Helpers;
using NextCommerceShop.Models;
using NextCommerceShop.Models.ViewModels;

namespace NextCommerceShop.Controllers
{
    public class ShopController : Controller
    {
        private readonly AppDbContext _context;

        public ShopController(AppDbContext context)
        {
            _context = context;
        }

        // ===========================
        // PUBLIC CATALOG
        // ===========================
        public async Task<IActionResult> Index(
            int? categoryId,
            string? search,
            string? sortBy,
            int page = 1,
            int pageSize = 9)
        {
            // Base query
            var query = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            // Category filter
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            // Search filter (case-insensitive, EF-safe)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();

                query = query.Where(p =>
                    EF.Functions.Like(p.Name, $"%{s}%") ||
                    (p.Description != null && EF.Functions.Like(p.Description, $"%{s}%")));
            }

            // Sorting
            query = sortBy switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name_asc" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),
                _ => query.OrderByDescending(p => p.Id) // default newest first
            };

            // Pagination safety
            if (page < 1) page = 1;

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (page > totalPages && totalPages > 0)
                page = totalPages;

            // Get paged data
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new CatalogViewModel
            {
                Products = products,
                Categories = await _context.Categories.ToListAsync(),

                SelectedCategoryId = categoryId,
                Search = search,
                SortBy = sortBy,

                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            };

            return View(vm);
        }

        // ===========================
        // PRODUCT DETAILS
        // ===========================
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        // ===========================
        // ADD TO CART
        // ===========================
        [HttpPost]
        public async Task<IActionResult> AddToCart(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            const string cartKey = "Cart";
            var cart = HttpContext.Session.GetObject<List<CartItem>>(cartKey)
                       ?? new List<CartItem>();

            var existing = cart.FirstOrDefault(c => c.ProductId == id);

            if (existing == null)
            {
                cart.Add(new CartItem
                {
                    ProductId = id,
                    Name = product.Name,
                    Price = product.Price,
                    Quantity = 1
                });
            }
            else
            {
                existing.Quantity += 1;
            }

            HttpContext.Session.SetObject(cartKey, cart);

            return RedirectToAction("Index", "Cart");
        }
    }
}
