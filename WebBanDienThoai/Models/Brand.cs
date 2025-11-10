using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WebBanDienThoai.Models
{
    [Table("Brands")]
    public class Brand
    {
        [Key]
        public int BrandId { get; set; }
        [Required]
        [StringLength(100)]
        public string BrandName { get; set; }
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}