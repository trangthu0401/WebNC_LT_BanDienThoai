using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WebBanDienThoai.Models
{
    [Table("ProductVariants")]
    public class ProductVariant
    {
        [Key] // <-- ĐÃ SỬA: Thêm [Key] để EF biết đây là khóa chính
        public int VariantId { get; set; }
        public int ProductId { get; set; }
        [StringLength(30)]
        public string Color { get; set; }
        [StringLength(20)]
        public string Storage { get; set; }
        [StringLength(20)]
        public string RAM { get; set; }
        [Column(TypeName = "DECIMAL(18,2)")]
        public decimal Price { get; set; }
        [Column(TypeName = "DECIMAL(18,2)")]
        public decimal? DiscountPrice { get; set; }
        public int Stock { get; set; }
        [StringLength(255)]
        public string ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<FavoriteDetail> FavoriteDetails { get; set; } = new List<FavoriteDetail>();
        public virtual ICollection<ReviewDetail> ReviewDetails { get; set; } = new List<ReviewDetail>();
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}