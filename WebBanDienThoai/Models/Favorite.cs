using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WebBanDienThoai.Models
{
    [Table("Favorites")]
    public class Favorite
    {
        [Key]
        public int FavoriteId { get; set; }
        public int? CustomerID { get; set; }
        public DateTime CreatedAt { get; set; }
        [ForeignKey("CustomerID")]
        public virtual Customer Customer { get; set; }
        public virtual ICollection<FavoriteDetail> FavoriteDetails { get; set; } = new List<FavoriteDetail>();
    }
}