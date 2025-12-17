using NextCommerceShop.Models;

namespace NextCommerceShop.ViewModels
{
    public class UserWithRolesViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public List<string> Roles { get; set; } = new();
    }
}
