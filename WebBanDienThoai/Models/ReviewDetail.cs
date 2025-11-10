using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WebBanDienThoai.Models
{
    [Table("ReviewDetails")]
    public class ReviewDetail
    {
        [Key]
        public int ReviewDetailId { get; set; }
        public int? ReviewId { get; set; }
        public int? VariantId { get; set; }
        public int? Rating { get; set; }
        public string Comment { get; set; }
        [ForeignKey("ReviewId")]
        public virtual Review Review { get; set; }
        [ForeignKey("VariantId")]
        public virtual ProductVariant Variant { get; set; }
    }
}