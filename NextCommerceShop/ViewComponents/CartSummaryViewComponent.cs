using Microsoft.AspNetCore.Mvc;
using NextCommerceShop.Helpers;
using NextCommerceShop.Models;

namespace NextCommerceShop.ViewComponents
{
    public class CartSummaryViewComponent : ViewComponent
    {
        private const string CartSessionKey = "Cart";

        public IViewComponentResult Invoke()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CartSessionKey)
                       ?? new List<CartItem>();

            int itemCount = cart.Sum(item => item.Quantity);

            return View(itemCount);
        }
    }
}
