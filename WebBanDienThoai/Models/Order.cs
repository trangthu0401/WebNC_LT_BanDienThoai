using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WebBanDienThoai.Models
{
    [Table("Orders")]
    public class Order
    {
        [Key]
        public int OrderId { get; set; }
        public int? CustomerID { get; set; }
        public DateTime? OrderDate { get; set; }
        [StringLength(50)]
        public string Status { get; set; }
        [Column(TypeName = "DECIMAL(18,2)")]
        public decimal? TotalAmount { get; set; }
        [ForeignKey("CustomerID")]
        public virtual Customer Customer { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ICollection<Shipping> Shippings { get; set; } = new List<Shipping>();
    }
}