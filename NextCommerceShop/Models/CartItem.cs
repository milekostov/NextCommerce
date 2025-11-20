namespace NextCommerceShop.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }      // which product
        public string Name { get; set; } = "";  // product name (for display)
        public decimal Price { get; set; }      // price at the moment of adding
        public int Quantity { get; set; }       // how many pieces
    }
}
