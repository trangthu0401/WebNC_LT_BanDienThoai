using Microsoft.EntityFrameworkCore;
using WebBanDienThoai.Models;

namespace WebBanDienThoai.Models
{
    public class DemoWebBanDienThoai1Context : DbContext
    {
        public DemoWebBanDienThoai1Context(DbContextOptions<DemoWebBanDienThoai1Context> options)
            : base(options)
        {
        }

        // Khai báo các DbSet (các bảng) của bạn
        public virtual DbSet<Customer> Customers { get; set; }
        public virtual DbSet<Address> Addresses { get; set; }
        public virtual DbSet<Account> Accounts { get; set; }
        public virtual DbSet<CartItem> CartItems { get; set; }
        public virtual DbSet<ProductVariant> ProductVariants { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<Favorite> Favorites { get; set; }
        public virtual DbSet<Review> Reviews { get; set; }
        // ... (Thêm các DbSet khác nếu bạn có) ...

        // === PHẦN SỬA LỖI QUAN TRỌNG NHẤT ===
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Luôn giữ dòng này

            // Báo cho EF Core biết tên bảng chính xác (VIẾT HOA) trong SQL Server
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.ToTable("CUSTOMER"); // <-- SỬA LẠI THÀNH VIẾT HOA
            });

            modelBuilder.Entity<Address>(entity =>
            {
                entity.ToTable("ADDRESS"); // <-- SỬA LẠI THÀNH VIẾT HOA
            });

            modelBuilder.Entity<Account>(entity =>
            {
                entity.ToTable("ACCOUNT"); // <-- SỬA LẠI THÀNH VIẾT HOA
            });

            // KHÔNG CẦN cấu hình cho CartItems, Products...
            // vì tên DbSet (ví dụ: CartItems) đã khớp với tên bảng SQL (CartItems)
        }
    }
}