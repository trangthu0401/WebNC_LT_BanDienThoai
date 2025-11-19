using Microsoft.EntityFrameworkCore;
using WebBanDienThoai.Models; 

namespace WebBanDienThoai.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Khai báo các DbSet (tương ứng với các bảng trong DB)
        public DbSet<Product> Products { get; set; } = default!;
        public DbSet<ProductVariant> ProductVariants { get; set; } = default!;
        public DbSet<Brand> Brands { get; set; } = default!;
    }
}