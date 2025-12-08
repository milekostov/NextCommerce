using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NextCommerceShop.Data;
using NextCommerceShop.Helpers;
using NextCommerceShop.Models;
using NextCommerceShop.Models.ViewModels;
using NextCommerceShop.Services.Payments;

namespace NextCommerceShop.Controllers
{
    public class ShopController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPaymentService _paymentService;


        public ShopController(AppDbContext context, IPaymentService paymentService  )
        {
            _context = context;
            _paymentService = paymentService;
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
        public async Task<IActionResult> Checkout()
        {
            const string cartKey = "Cart";
            var cart = HttpContext.Session.GetObject<List<CartItem>>(cartKey) ?? new List<CartItem>();

            if (!cart.Any())
                return RedirectToAction("Index");

            var vm = new CheckoutViewModel
            {
                CartItems = cart,
                TotalAmount = cart.Sum(c => c.Price * c.Quantity)
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            const string cartKey = "Cart";
            var cart = HttpContext.Session.GetObject<List<CartItem>>(cartKey) ?? new List<CartItem>();

            if (!cart.Any())
                return RedirectToAction("Index", "Shop"); // Cart empty

            var totalAmount = cart.Sum(c => c.Price * c.Quantity);

            // 1) Create Order
            var order = new Order
            {
                FullName = model.FullName,
                Address = model.Address,
                City = model.City,
                Phone = model.Phone,
                Email = model.Email,
                TotalAmount = totalAmount,
                Status = OrderStatus.Pending
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // 2) Create OrderItems
            foreach (var item in cart)
            {
                _context.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price
                });
            }
            await _context.SaveChangesAsync();

            // 3) Create PaymentRequest
            var paymentRequest = new PaymentRequest
            {
                OrderId = order.Id,
                Amount = order.TotalAmount,
                Currency = "MKD",
                Description = $"Order #{order.Id}"
            };

            // 4) Redirect to payment provider
            // Replace "StubProvider" with real provider later
            var redirectUrl = await _paymentService.CreatePaymentAsync("StubProvider", paymentRequest);

            return Redirect(redirectUrl);
        }

    }
}
