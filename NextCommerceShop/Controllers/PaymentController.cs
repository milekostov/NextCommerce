using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextCommerceShop.Data;
using NextCommerceShop.Models;
using NextCommerceShop.Services;
using NextCommerceShop.Services.Payments;
using System.Linq;

namespace NextCommerceShop.Controllers
{
    public class PaymentController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IPaymentService _paymentService;

        public PaymentController(AppDbContext db, IPaymentService paymentService)
        {
            _db = db;
            _paymentService = paymentService;
        }

        [HttpGet]
        public async Task<IActionResult> Callback()
        {
            // Gather query parameters from the provider
            var parameters = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());

            // Verify payment using PaymentService
            // Use the provider prefix that matches the provider class name (StubProvider -> "Stub")
            var result = await _paymentService.VerifyAsync("Stub", parameters);

            // Find the order
            var orderId = int.Parse(parameters["orderId"]); // assumes provider returns your orderId
            var order = await _db.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            // Save PaymentTransaction
            var transaction = new PaymentTransaction
            {
                OrderId = order.Id,
                Amount = order.TotalAmount,
                Currency = "MKD",
                Provider = "ProviderName",
                ProviderTransactionId = result.ProviderTransactionId ?? "",
                RawResponse = result.RawResponse,
                Success = result.Success,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };
            _db.PaymentTransactions.Add(transaction);

            // Update order status and log history
            var oldStatus = order.Status;
            order.Status = result.Success ? OrderStatus.Paid : OrderStatus.Pending;

            var history = new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = oldStatus,
                ToStatus = order.Status,
                ChangedAt = DateTime.UtcNow
            };
            _db.OrderStatusHistories.Add(history);

            await _db.SaveChangesAsync();

            // Show result to customer
            return result.Success
                ? View("PaymentSuccess", order)
                : View("PaymentFailed", order);
        }
    }
}