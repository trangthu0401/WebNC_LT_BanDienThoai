using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebBanDienThoai.Models
{
    public partial class Account
    {
        [Key]
        public int AccountID { get; set; } // Sửa: Khớp CSDL (chữ hoa)

        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "Customer";

        public DateTime? CreatedAt { get; set; } // Sửa: Khớp CSDL (cho phép null)

        public bool? IsActive { get; set; }

        // Các thuộc tính liên kết (Navigation properties)
        public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}