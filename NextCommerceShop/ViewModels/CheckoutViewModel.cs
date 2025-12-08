using System.Collections.Generic;

namespace NextCommerceShop.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public List<CartItem> CartItems { get; set; } = new();
        public decimal TotalAmount { get; set; }

        // Customer fields
        public string FullName { get; set; } = "";
        public string Address { get; set; } = "";
        public string City { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
    }
}
