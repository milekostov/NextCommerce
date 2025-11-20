namespace NextCommerceShop.Models
{
    public class Product
    {
        public int Id { get; set; }              // unique number for each product
        public string Name { get; set; } = "";   // product name
        public string? Description { get; set; } // optional description
        public decimal Price { get; set; }       // price of the product
        public int StockQuantity { get; set; }   // how many items we have in stock

        public int? CategoryId { get; set; }      // which category it belongs to (nullable for now)
        public Category? Category { get; set; }   // navigation property
        public string? ImageUrl { get; set; }

    }
}
