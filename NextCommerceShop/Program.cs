using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NextCommerceShop.Application.Abstractions;
using NextCommerceShop.Data;
using NextCommerceShop.Infrastructure.Auditing;
using NextCommerceShop.Infrastructure.Identity;
using NextCommerceShop.Models;
using NextCommerceShop.Models.Settings;
using NextCommerceShop.Services;
using NextCommerceShop.Services.Payments;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------
// Bind strongly typed settings
// ----------------------------------------
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<AdminUserSettings>(builder.Configuration.GetSection("AdminUser"));

// MVC Controllers + Views
builder.Services.AddControllersWithViews();

// Identity
builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.User.RequireUniqueEmail = true;

        // ✅ REQUIRED for Admin lock / unlock
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

// Auditing
builder.Services.AddScoped<IActorContext, HttpActorContext>();
builder.Services.AddScoped<IAuditService, AuditService>();

builder.Services.AddTransient<IEmailSender, BrevoEmailSender>();

builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();

// EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Payments
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddSingleton<IPaymentProvider, StubProvider>();

// Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();
await SeedAdminAsync(app.Services);

async Task SeedAdminAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var adminSettings = scope.ServiceProvider.GetRequiredService<IOptions<AdminUserSettings>>().Value;

    const string adminRole = "Admin";

    if (!await roleManager.RoleExistsAsync(adminRole))
        await roleManager.CreateAsync(new IdentityRole(adminRole));

    if (string.IsNullOrWhiteSpace(adminSettings.Email) || string.IsNullOrWhiteSpace(adminSettings.Password))
        throw new Exception("AdminUser settings are missing in appsettings or UserSecrets.");

    var adminUser = await userManager.FindByEmailAsync(adminSettings.Email);

    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminSettings.Email,
            Email = adminSettings.Email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, adminSettings.Password);

        if (!result.Succeeded)
            throw new Exception("Failed to create admin: " +
                                string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    if (!await userManager.IsInRoleAsync(adminUser, adminRole))
        await userManager.AddToRoleAsync(adminUser, adminRole);
}

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Shop}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
