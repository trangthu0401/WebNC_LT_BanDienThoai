using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WebBanDienThoai.Models
{
    [Table("Reviews")]
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }
        public int? CustomerID { get; set; }
        public DateTime CreatedAt { get; set; }
        [ForeignKey("CustomerID")]
        public virtual Customer Customer { get; set; }
        public virtual ICollection<ReviewDetail> ReviewDetails { get; set; } = new List<ReviewDetail>();
    }
}