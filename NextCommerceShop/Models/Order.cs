using System.ComponentModel.DataAnnotations;

namespace NextCommerceShop.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Address { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string City { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string Phone { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public decimal TotalAmount { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        // 🔗 link to user (for My Orders)
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        // Items
        public List<OrderItem> Items { get; set; } = new();
        public List<OrderStatusHistory> StatusHistory { get; set; } = new();

    }
}
