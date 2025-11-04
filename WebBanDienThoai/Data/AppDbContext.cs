using Microsoft.EntityFrameworkCore;
using WebBanDienThoai.Models; // <-- Đảm bảo đây là namespace của bạn

namespace WebBanDienThoai.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Tên DbSet (Accounts) là tên bạn dùng trong C#
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Customer> Customers { get; set; }

        // (QUAN TRỌNG NHẤT) Ghi đè phương thức này
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình cho Bảng 1: ACCOUNT
            modelBuilder.Entity<Account>(entity =>
            {
                // Dòng này báo EF Core: "Hãy dùng bảng tên 'ACCOUNT'"
                entity.ToTable("ACCOUNT");

                // (Các cấu hình khác... giữ nguyên)
                entity.HasKey(a => a.AccountID);
                entity.Property(a => a.Email).IsRequired().HasMaxLength(100);
                entity.HasIndex(a => a.Email).IsUnique();
                // ... (Các cấu hình khác cho Account)
            });

            // Cấu hình cho Bảng 2: CUSTOMER
            modelBuilder.Entity<Customer>(entity =>
            {
                // Dòng này báo EF Core: "Hãy dùng bảng tên 'CUSTOMER'"
                entity.ToTable("CUSTOMER");

                // (Các cấu hình khác cho Customer... giữ nguyên)
                entity.HasKey(c => c.CustomerID);
                entity.HasOne(c => c.Account)
                      .WithOne()
                      .HasForeignKey<Customer>(c => c.AccountID)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}