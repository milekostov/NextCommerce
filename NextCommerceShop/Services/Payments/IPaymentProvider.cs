using System.Threading.Tasks;

namespace NextCommerceShop.Services.Payments
{
    public interface IPaymentProvider
    {
        /// <summary>
        /// Builds a redirect/payment URL or HTML form that the client will be sent to.
        /// </summary>
        Task<string> CreatePaymentUrlAsync(PaymentRequest request);

        /// <summary>
        /// Validates the callback/response from the bank.
        /// </summary>
        Task<PaymentResult> VerifyAsync(Dictionary<string, string> parameters);
    }
}
