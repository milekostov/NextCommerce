using Microsoft.AspNetCore.Mvc;
using NextCommerceShop.Helpers;
using NextCommerceShop.Models;
using System.Linq;
using NextCommerceShop.Data;

namespace NextCommerceShop.Controllers
{
    public class CartController : Controller
    {
        private const string CartSessionKey = "Cart";

        // 🔹 DB context
        private readonly AppDbContext _context;

        // 🔹 Constructor – ASP.NET injects AppDbContext here
        public CartController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Load cart from session, or create empty list if nothing there
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CartSessionKey)
                       ?? new List<CartItem>();

            return View(cart);
        }

        // 🔹 NEW: Add to cart
        [HttpPost]
        public IActionResult Add(int productId)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null)
            {
                return RedirectToAction("Index", "Shop");
            }

            var cart = HttpContext.Session.GetObject<List<CartItem>>(CartSessionKey)
                       ?? new List<CartItem>();

            var existingItem = cart.FirstOrDefault(c => c.ProductId == productId);
            if (existingItem != null)
            {
                existingItem.Quantity += 1;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    Quantity = 1
                });
            }

            HttpContext.Session.SetObject(CartSessionKey, cart);

            return RedirectToAction("Index", "Shop");
        }

        [HttpPost]
        public IActionResult Remove(int productId)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CartSessionKey)
                       ?? new List<CartItem>();

            var item = cart.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                cart.Remove(item);
                HttpContext.Session.SetObject(CartSessionKey, cart);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Increase(int productId)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CartSessionKey)
                       ?? new List<CartItem>();

            var item = cart.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                item.Quantity += 1;
                HttpContext.Session.SetObject(CartSessionKey, cart);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Decrease(int productId)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CartSessionKey)
                       ?? new List<CartItem>();

            var item = cart.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                if (item.Quantity > 1)
                {
                    item.Quantity -= 1;
                }
                else
                {
                    cart.Remove(item);
                }

                HttpContext.Session.SetObject(CartSessionKey, cart);
            }

            return RedirectToAction("Index");
        }
    }
}
