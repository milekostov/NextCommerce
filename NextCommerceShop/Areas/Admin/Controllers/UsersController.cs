using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NextCommerceShop.Models;
using NextCommerceShop.ViewModels;
using NextCommerceShop.ViewModels.Admin;
using NextCommerceShop.Application.Abstractions;


namespace NextCommerceShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IAuditService _audit;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IAuditService audit)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _audit = audit;
        }


        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var model = new List<UserWithRolesViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                model.Add(new UserWithRolesViewModel
                {
                    User = user,
                    Roles = roles.ToList()
                });
            }

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteToAdmin(ChangeUserRoleRequest req)
        {
            var user = await _userManager.FindByIdAsync(req.UserId);
            if (user == null) return NotFound();

            if (!await _roleManager.RoleExistsAsync("Admin"))
                await _roleManager.CreateAsync(new IdentityRole("Admin"));

            if (!await _userManager.IsInRoleAsync(user, "Admin"))
            {
                await _userManager.AddToRoleAsync(user, "Admin");
                TempData["Success"] = $"User '{user.Email}' has been promoted to Admin.";
            }

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAdmin(ChangeUserRoleRequest req)
        {
            var user = await _userManager.FindByIdAsync(req.UserId);
            if (user == null) return NotFound();

            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            if (admins.Count == 1 && admins[0].Id == user.Id)
            {
                TempData["Error"] = "You cannot remove the last admin.";
                return RedirectToAction(nameof(Index));
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Admin");
                TempData["Success"] = $"Admin role removed from '{user.Email}'.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Lock(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            // Prevent admin from locking themselves
            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["Error"] = "You cannot lock your own account.";
                return RedirectToAction(nameof(Index));
            }

            await _userManager.SetLockoutEndDateAsync(
                user,
                DateTimeOffset.UtcNow.AddYears(100)
            );

            await _audit.WriteAsync(
                action: "Admin.User.Locked",
                targetUserId: user.Id,
                targetEmail: user.Email
            );

            TempData["Success"] = $"User '{user.Email}' has been locked.";

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            await _userManager.SetLockoutEndDateAsync(user, null);

            await _audit.WriteAsync(
                action: "Admin.User.Unlocked",
                targetUserId: user.Id,
                targetEmail: user.Email
            );

            TempData["Success"] = $"User '{user.Email}' has been unlocked.";

            return RedirectToAction(nameof(Index));
        }


    }
}
