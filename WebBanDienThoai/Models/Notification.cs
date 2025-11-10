using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WebBanDienThoai.Models
{
    [Table("Notifications")]
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }
        public int? AccountID { get; set; }
        [StringLength(255)]
        public string Message { get; set; }
        [StringLength(50)]
        public string Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        [ForeignKey("AccountID")]
        public virtual Account Account { get; set; }
    }
}