using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextCommerceShop.Data;
using NextCommerceShop.Models;
using Microsoft.EntityFrameworkCore;

namespace NextCommerceShop.Services.Payments
{
    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _db;
        private readonly IEnumerable<IPaymentProvider> _providers;

        public PaymentService(AppDbContext db, IEnumerable<IPaymentProvider> providers)
        {
            _db = db;
            _providers = providers;
        }

        private IPaymentProvider GetProvider(string providerName)
        {
            var provider = _providers.FirstOrDefault(p =>
                p.GetType().Name.StartsWith(providerName, StringComparison.OrdinalIgnoreCase));

            if (provider == null)
                throw new Exception($"Payment provider '{providerName}' not found.");

            return provider;
        }

        public async Task<string> CreatePaymentAsync(string providerName, PaymentRequest request)
        {
            var provider = GetProvider(providerName);
            return await provider.CreatePaymentUrlAsync(request);
        }

        public async Task<PaymentResult> VerifyAsync(string providerName, Dictionary<string, string> parameters)
        {
            var provider = GetProvider(providerName);
            return await provider.VerifyAsync(parameters);
        }

        public async Task<PaymentResult> HandlePaymentCallbackAsync(string providerName, string transactionId)
        {
            // Simple bridge: translate transactionId into parameters expected by providers.
            var parameters = new Dictionary<string, string>
            {
                ["transactionId"] = transactionId
            };

            return await VerifyAsync(providerName, parameters);
        }
    }
}
