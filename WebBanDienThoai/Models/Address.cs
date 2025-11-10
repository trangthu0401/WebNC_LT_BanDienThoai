using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WebBanDienThoai.Models
{
    [Table("ADDRESS")]
    public class Address
    {
        [Key]
        public int AddressID { get; set; }
        public int CustomerID { get; set; }
        [Required]
        [StringLength(255)]
        public string Street { get; set; }
        [StringLength(100)]
        public string District { get; set; }
        [StringLength(100)]
        public string City { get; set; }
        [StringLength(100)]
        public string Country { get; set; }
        [StringLength(20)]
        public string PostalCode { get; set; }
        public bool IsDefault { get; set; }
        [ForeignKey("CustomerID")]
        public virtual Customer Customer { get; set; }
    }
}