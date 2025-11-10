using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
namespace WebBanDienThoai.Models
{
    [Table("CUSTOMER")]
    public class Customer
    {
        [Key]
        public int CustomerID { get; set; }
        public int AccountID { get; set; }
        [Required]
        [StringLength(100)]
        public string FullName { get; set; }
        [StringLength(15)]
        public string Phone { get; set; }
        [StringLength(10)]
        public string Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        [StringLength(20)]
        public string CustomerType { get; set; }
        [ForeignKey("AccountID")]
        public virtual Account Account { get; set; }
        public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}