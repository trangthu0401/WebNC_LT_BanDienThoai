using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WebBanDienThoai.Data;
using WebBanDienThoai.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Kết nối DB
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DemoWebBanDienThoaiDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Services
builder.Services.AddControllersWithViews();
builder.Services.AddTransient<EmailSender>();
// 3. AUTHENTICATION (QUAN TRỌNG)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.Name = "RabitStoreAuth"; // Đặt tên riêng để tránh xung đột
    });

var app = builder.Build();

// 4. Middleware Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Thứ tự bắt buộc: Authen -> Author
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "profile",
    pattern: "Account/Profile",
    defaults: new { controller = "Account", action = "Profile" });

// Route mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();