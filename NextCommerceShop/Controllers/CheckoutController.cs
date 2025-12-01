using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NextCommerceShop.Data;
using NextCommerceShop.Helpers;
using NextCommerceShop.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity;


namespace NextCommerceShop.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<ApplicationUser> _userManager;

        private const string CartSessionKey = "Cart";

        public CheckoutController(
            AppDbContext context,
            IEmailSender emailSender,
            UserManager<ApplicationUser> userManager)   // ✅ add this parameter
        {
            _context = context;
            _emailSender = emailSender;
            _userManager = userManager;                 // ✅ now this is valid
        }

        // 1) Show checkout form
        [HttpGet]
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CartSessionKey)
                       ?? new List<CartItem>();

            if (!cart.Any())
                return RedirectToAction("Index", "Cart");

            return View(new Order());
        }

        // 2) Place order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(Order order)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CartSessionKey)
                       ?? new List<CartItem>();

            if (!cart.Any())
                return RedirectToAction("Index", "Cart");

            if (!ModelState.IsValid)
                return View("Index", order);
            // Ensure server-controlled values
            order.CreatedAt = DateTime.UtcNow;
            order.Status = OrderStatus.Pending;

            // Link order to logged-in user (for My Orders)
            if (User.Identity?.IsAuthenticated == true)
            {
                order.UserId = _userManager.GetUserId(User);
            }


            // Calculate total
            order.TotalAmount = cart.Sum(i => i.Price * i.Quantity);

            // Build order items
            order.Items = cart.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.Name ?? "",
                UnitPrice = i.Price,
                Quantity = i.Quantity
            }).ToList();

            _context.Orders.Add(order);

            // Reduce stock
            foreach (var ci in cart)
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == ci.ProductId);
                if (product != null)
                {
                    product.StockQuantity = Math.Max(0, product.StockQuantity - ci.Quantity);
                }
            }

            await _context.SaveChangesAsync();

            // Clear cart no matter what happens next
            HttpContext.Session.Remove(CartSessionKey);

            // Send email (but don't break order if SMTP fails)
            try
            {
                var itemsHtml = string.Join("", order.Items.Select(i =>
                    $"<li>{i.ProductName} — {i.Quantity} x {i.UnitPrice:0.00} €</li>"
                ));

                var body = $@"
<h2>Thanks for your order!</h2>
<p>Your order <strong>#{order.Id}</strong> was received.</p>
<p><strong>Total:</strong> {order.TotalAmount:0.00} €</p>
<ul>{itemsHtml}</ul>
<p>We’ll contact you when it ships.</p>
";

                await _emailSender.SendEmailAsync(
                    order.Email,
                    $"Order Confirmation #{order.Id}",
                    body
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine("Email send failed: " + ex.Message);
            }

            return RedirectToAction("Success", new { orderId = order.Id });
        }

        // 3) Success page (MISSING in your file — adding here)
        public async Task<IActionResult> Success(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return NotFound();

            return View(order);
        }
    }
}
