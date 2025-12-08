namespace NextCommerceShop.Services.Payments
{
    public class PaymentResult
    {
        public bool Success { get; set; }
        public string? ProviderTransactionId { get; set; }
        public string? RawResponse { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
