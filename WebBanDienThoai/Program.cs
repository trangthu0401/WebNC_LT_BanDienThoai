using Microsoft.EntityFrameworkCore;
using WebBanDienThoai.Models; // Namespace chứa DbContext của bạn

var builder = WebApplication.CreateBuilder(args);

// 1. Lấy chuỗi kết nối
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. Đăng ký DbContext
builder.Services.AddDbContext<DemoWebBanDienThoaiContext>(options =>
    options.UseSqlServer(connectionString));

// Thêm dịch vụ Controller và View
builder.Services.AddControllersWithViews();

var app = builder.Build();

// --- PHẦN CẤU HÌNH BỊ THIẾU NẰM Ở ĐÂY ---

// Cấu hình đường ống (pipeline) cho HTTP request
// Bật trang báo lỗi chi tiết khi đang phát triển (Development)
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// DÒNG QUAN TRỌNG: Cho phép tải file tĩnh (CSS, JS, Ảnh)
app.UseStaticFiles();

// DÒNG QUAN TRỌNG: Kích hoạt hệ thống định tuyến (Routing)
app.UseRouting();

// (Tùy chọn) Bật tính năng xác thực/phân quyền (nếu có đăng nhập)
app.UseAuthorization();

// --- KẾT THÚC PHẦN BỊ THIẾU ---


// 6. Ánh xạ route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();