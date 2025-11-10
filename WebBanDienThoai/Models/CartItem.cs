using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WebBanDienThoai.Models
{
    [Table("CartItems")] // <-- ĐÃ SỬA: Khớp với tên bảng SQL
    public class CartItem
    {
        [Key]
        public int CartItemId { get; set; }
        public int? CustomerID { get; set; } // int? cho phép null
        public int? VariantId { get; set; } // int? cho phép null
        public int Quantity { get; set; }
        [ForeignKey("CustomerID")]
        public virtual Customer Customer { get; set; }
        [ForeignKey("VariantId")]
        public virtual ProductVariant Variant { get; set; } // <-- ĐÃ SỬA: Phải là 'virtual'
    }
}