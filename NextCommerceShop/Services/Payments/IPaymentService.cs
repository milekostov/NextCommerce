using System.Collections.Generic;
using System.Threading.Tasks;
using NextCommerceShop.Models;

namespace NextCommerceShop.Services.Payments
{
    public interface IPaymentService
    {
        Task<string> CreatePaymentAsync(string providerName, PaymentRequest request);

        Task<PaymentResult> HandlePaymentCallbackAsync(string providerName, string transactionId);

        // Added to match PaymentService.VerifyAsync(...)
        Task<PaymentResult> VerifyAsync(string providerName, Dictionary<string, string> parameters);
    }
}
