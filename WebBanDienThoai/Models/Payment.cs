using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WebBanDienThoai.Models
{
    [Table("Payments")]
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }
        public int? OrderId { get; set; }
        [StringLength(50)]
        public string PaymentMethod { get; set; }
        public DateTime? PaymentDate { get; set; }
        [StringLength(50)]
        public string PaymentStatus { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }
    }
}