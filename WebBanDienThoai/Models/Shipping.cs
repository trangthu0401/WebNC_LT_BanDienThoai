using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WebBanDienThoai.Models
{
    [Table("Shipping")]
    public class Shipping
    {
        [Key]
        public int ShippingId { get; set; }
        public int? OrderId { get; set; }
        [StringLength(100)]
        public string Carrier { get; set; }
        [StringLength(100)]
        public string TrackingNumber { get; set; }
        public DateTime? ShippedDate { get; set; }
        public DateTime? EstimatedDelivery { get; set; }
        public DateTime? DeliveredDate { get; set; }
        [StringLength(50)]
        public string Status { get; set; }
        public string Note { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }
    }
}