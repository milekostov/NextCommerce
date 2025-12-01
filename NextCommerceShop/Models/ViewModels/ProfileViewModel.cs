using System.ComponentModel.DataAnnotations;

namespace NextCommerceShop.ViewModels
{
    public class ProfileViewModel
    {
        [Required, MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = "";

        [Required, EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";
    }
}
