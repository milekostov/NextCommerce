namespace NextCommerceShop.Models
{
    public class Category
    {
        public int Id { get; set; }              // unique ID for the category
        public string Name { get; set; } = "";   // category name, e.g. "Electronics"

        // Navigation property - list of products in this category
        public List<Product> Products { get; set; } = new();
    }
}
