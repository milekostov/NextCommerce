using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using NextCommerceShop.Data;
using NextCommerceShop.Models;
using NextCommerceShop.Services;
using NextCommerceShop.Services.Payments;

var builder = WebApplication.CreateBuilder(args);

// MVC Controllers + Views
builder.Services.AddControllersWithViews();

// Identity (with required email confirmation)
builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddTransient<IEmailSender, BrevoEmailSender>();

builder.Services.AddRazorPages();

builder.Services.AddHttpContextAccessor();

// EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register IPaymentService -> PaymentService
builder.Services.AddScoped<IPaymentService, PaymentService>();

// register the concrete provider(s)
builder.Services.AddSingleton<IPaymentProvider, StubProvider>();

// Session (Cart)
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

    const string adminRole = "Admin";

    // 1) Create Admin role if missing
    if (!await roleManager.RoleExistsAsync(adminRole))
        await roleManager.CreateAsync(new IdentityRole(adminRole));

    // 2) Promote this email to Admin (you will change this in Step 3)
    var adminEmail = "mkostov@nextbyte.mk";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser != null && !await userManager.IsInRoleAsync(adminUser, adminRole))
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
app.UseAuthentication();     // <-- IMPORTANT
app.UseAuthorization();

// MVC

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Shop}/{action=Index}/{id?}");

// Identity Razor Pages
app.MapRazorPages();

app.Run();
