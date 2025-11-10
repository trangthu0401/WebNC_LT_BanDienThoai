using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WebBanDienThoai.Models
{
    [Table("FavoriteDetails")]
    public class FavoriteDetail
    {
        [Key]
        public int FavoriteDetailId { get; set; }
        public int? FavoriteId { get; set; }
        public int? VariantId { get; set; }
        [ForeignKey("FavoriteId")]
        public virtual Favorite Favorite { get; set; }
        [ForeignKey("VariantId")]
        public virtual ProductVariant Variant { get; set; }
    }
}