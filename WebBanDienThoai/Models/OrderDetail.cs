using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WebBanDienThoai.Models
{
    [Table("OrderDetails")]
    public class OrderDetail
    {
        [Key]
        public int OrderDetailId { get; set; }
        public int? OrderId { get; set; }
        public int? VariantId { get; set; }
        public int? Quantity { get; set; }
        [Column(TypeName = "DECIMAL(18,2)")]
        public decimal? UnitPrice { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }
        [ForeignKey("VariantId")]
        public virtual ProductVariant Variant { get; set; }
    }
}