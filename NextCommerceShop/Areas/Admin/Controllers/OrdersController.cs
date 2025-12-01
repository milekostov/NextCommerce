using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NextCommerceShop.Data;
using NextCommerceShop.Models;

namespace NextCommerceShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrdersController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender? _emailSender; // optional, may be null if not registered

        public OrdersController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailSender? emailSender = null) // allow omission for projects without email
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        // GET: /Admin/Orders
        // Simple list (filter by search (id/name/email/phone) and status).
        public async Task<IActionResult> Index(string? search, OrderStatus? status)
        {
            var query = _context.Orders
                .Include(o => o.Items)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(o =>
                    o.Id.ToString().Contains(search) ||
                    o.FullName.Contains(search) ||
                    o.Email.Contains(search) ||
                    o.Phone.Contains(search));
            }

            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(orders);
        }

        // GET: /Admin/Orders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            // load histories for this order
            var histories = await _context.OrderStatusHistories
                .Where(h => h.OrderId == id)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();

            // build a map userId -> display name
            var userIds = histories
                .Select(h => h.ChangedByUserId)
                .Where(uid => !string.IsNullOrEmpty(uid))
                .Distinct()
                .ToList();

            var nameMap = new Dictionary<string, string>();

            foreach (var uid in userIds)
            {
                var user = await _userManager.FindByIdAsync(uid);

                if (user != null)
                {
                    string display =
                        !string.IsNullOrWhiteSpace(user.FullName)
                            ? user.FullName!                 // safe because we checked
                            : user.Email ?? "Unknown user";   // fallback

                    nameMap[uid] = display;
                }
                else
                {
                    nameMap[uid] = "Unknown user";
                }
            }


            return View(order);
        }


        // POST: /Admin/Orders/UpdateStatus (server POST fallback)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            // Only record a history entry if the status changes
            if (order.Status != newStatus)
            {
                var changedBy = _userManager.GetUserId(User) ?? "system";

                var history = new OrderStatusHistory
                {
                    OrderId = id,
                    FromStatus = order.Status,
                    ToStatus = newStatus,
                    ChangedAt = DateTime.UtcNow,
                    ChangedByUserId = changedBy
                };

                await _context.OrderStatusHistories.AddAsync(history);
            }

            // Update the order status
            order.Status = newStatus;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Order #{id} status updated to {newStatus}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // === AJAX endpoint (recommended UX) ===
        public class UpdateStatusRequest
        {
            public int Id { get; set; }
            public OrderStatus Status { get; set; }
            public string? Note { get; set; }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatusAjax([FromBody] UpdateStatusRequest req)
        {
            if (req == null) return BadRequest(new { success = false, message = "Invalid payload." });

            var order = await _context.Orders.FindAsync(req.Id);
            if (order == null) return NotFound(new { success = false, message = "Order not found." });

            if (!Enum.IsDefined(typeof(OrderStatus), req.Status))
                return BadRequest(new { success = false, message = "Invalid status." });

            var newStatus = req.Status;
            var oldStatus = order.Status;

            // Validate allowed transition (adjust rules as needed)
            if (!IsAllowedTransition(oldStatus, newStatus))
            {
                return BadRequest(new { success = false, message = "Invalid status transition." });
            }

            if (oldStatus != newStatus)
            {
                // update status
                order.Status = newStatus;

                var changedBy = _userManager.GetUserId(User) ?? "system";

                var history = new OrderStatusHistory
                {
                    OrderId = order.Id,
                    FromStatus = oldStatus,
                    ToStatus = newStatus,
                    ChangedAt = DateTime.UtcNow,
                    ChangedByUserId = changedBy,
                    Note = req.Note
                };

                _context.OrderStatusHistories.Add(history);

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    return StatusCode(500, new { success = false, message = "Database error while saving status." });
                }

                // optional: notify customer (don't fail if email service errors)
                if (_emailSender != null && !string.IsNullOrWhiteSpace(order.Email))
                {
                    try
                    {
                        var subject = $"Order #{order.Id} status updated to {newStatus}";
                        var body = $"Hello {order.FullName},\n\nYour order #{order.Id} status is now: {newStatus}.\n\nThank you.";
                        await _emailSender.SendEmailAsync(order.Email, subject, body);
                    }
                    catch
                    {
                        // swallow email errors (log in real app)
                    }
                }
            }

            return Ok(new { success = true, message = $"Status updated to {newStatus}.", newStatus = newStatus.ToString() });
        }

        // Allowed transitions - adjust to your business rules
        private bool IsAllowedTransition(OrderStatus from, OrderStatus to)
        {
            if (from == to) return true;

            var allowed = new Dictionary<OrderStatus, OrderStatus[]>
            {
                { OrderStatus.Pending,     new[] { OrderStatus.Processing, OrderStatus.Cancelled } },
                { OrderStatus.Processing,  new[] { OrderStatus.Shipped, OrderStatus.Cancelled } },
                { OrderStatus.Shipped,     new[] { OrderStatus.Completed, OrderStatus.Refunded } },
                { OrderStatus.Paid,        new[] { OrderStatus.Processing, OrderStatus.Shipped, OrderStatus.Cancelled } },
                // Completed, Cancelled, Refunded are terminal by default
            };

            if (!allowed.ContainsKey(from)) return false;
            return allowed[from].Contains(to);
        }
    }
}
