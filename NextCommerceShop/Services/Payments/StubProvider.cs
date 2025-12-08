using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextCommerceShop.Services.Payments
{
    public class StubProvider : IPaymentProvider
    {
        public Task<string> CreatePaymentUrlAsync(PaymentRequest request)
        {
            // Return a simple local callback URL including a fake transaction id
            var tx = Guid.NewGuid().ToString("N");
            var url = $"/Payment/Callback?orderId={request.OrderId}&transactionId={tx}";
            return Task.FromResult(url);
        }

        public Task<PaymentResult> VerifyAsync(Dictionary<string, string> parameters)
        {
            parameters ??= new Dictionary<string, string>();
            parameters.TryGetValue("transactionId", out var tx);

            var success = !string.IsNullOrEmpty(tx);

            return Task.FromResult(new PaymentResult
            {
                Success = success,
                ProviderTransactionId = tx,
                RawResponse = success ? $"{{\"transactionId\":\"{tx}\"}}" : string.Join(";", parameters),
                ErrorMessage = success ? null : "Missing transactionId"
            });
        }
    }
}