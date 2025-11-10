using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WebBanDienThoai.Models
{
    [Table("Products")]
    public class Product
    {
        [Key]
        public int ProductId { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        public int BrandId { get; set; }
        [StringLength(50)]
        public string Chipset { get; set; }
        [StringLength(30)]
        public string OperatingSystem { get; set; }
        public short? BatteryCapacity { get; set; }
        public bool ChargerIncluded { get; set; }
        [Column(TypeName = "DECIMAL(4,2)")]
        public decimal? ScreenSize { get; set; }
        [StringLength(40)]
        public string ScreenTech { get; set; }
        public short? RefreshRate { get; set; }
        [StringLength(100)]
        public string RearCamera { get; set; }
        [StringLength(50)]
        public string FrontCamera { get; set; }
        [Column(TypeName = "DECIMAL(5,2)")]
        public decimal? Weight { get; set; }
        [StringLength(50)]
        public string Dimensions { get; set; }
        public string Description { get; set; }
        public DateTime? ReleaseDate { get; set; }
        [StringLength(255)]
        public string MainImage { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        [ForeignKey("BrandId")]
        public virtual Brand Brand { get; set; }
        public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
    }
}