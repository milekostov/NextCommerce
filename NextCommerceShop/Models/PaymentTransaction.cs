using System.ComponentModel.DataAnnotations;

namespace NextCommerceShop.Models
{
    public class PaymentTransaction
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        [Required, MaxLength(50)]
        public string Provider { get; set; } = ""; // e.g., "Casys", "Stripe", "PayPal"

        [Required, MaxLength(100)]
        public string MerchantOrderId { get; set; } = ""; // your system order identifier

        [MaxLength(100)]
        public string ProviderTransactionId { get; set; } = ""; // returned by provider

        [Required]
        public decimal Amount { get; set; }

        [Required, MaxLength(10)]
        public string Currency { get; set; } = "MKD"; // or USD, EUR, etc.

        public bool Success { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        public string? RawResponse { get; set; } // full provider response for debugging
    }
}
