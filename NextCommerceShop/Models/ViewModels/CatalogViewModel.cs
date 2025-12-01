namespace NextCommerceShop.Models.ViewModels
{
    public class CatalogViewModel
    {
        public IEnumerable<Product> Products { get; set; } = Enumerable.Empty<Product>();
        public IEnumerable<Category> Categories { get; set; } = Enumerable.Empty<Category>();

        public int? SelectedCategoryId { get; set; }
        public string? Search { get; set; }
        public string? SortBy { get; set; }

        public int Page { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
    }
}
