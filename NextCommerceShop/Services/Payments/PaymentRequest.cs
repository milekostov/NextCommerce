namespace NextCommerceShop.Services.Payments
{
    public class PaymentRequest
    {
        public int OrderId { get; set; }

        public decimal Amount { get; set; }

        public string Currency { get; set; } = "MKD";

        public string Description { get; set; } = string.Empty;
    }
}
