using System.ComponentModel.DataAnnotations;

namespace NextCommerceShop.Models
{
    public class OrderStatusHistory
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        [Required, MaxLength(100)]
        public string ChangedByUserId { get; set; } = "";

        [Required]
        public OrderStatus FromStatus { get; set; }

        [Required]
        public OrderStatus ToStatus { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        public string? Note { get; set; }
    }
}
