using System;
using System.Collections.Generic;

namespace WebBanDienThoai.Models
{
    public partial class Customer
    {
        public int CustomerID { get; set; } // Sửa: Khớp CSDL (chữ hoa)

        public int AccountID { get; set; } // Sửa: Khớp CSDL (chữ hoa và bỏ '?')

        public string FullName { get; set; } = string.Empty;

        public string? Phone { get; set; }

        // Sửa: Khớp CSDL (dùng DateTime? vì CSDL dùng kiểu DATE)
        public DateTime? BirthDate { get; set; }

        public string Gender { get; set; } = string.Empty;

        public string CustomerType { get; set; } = string.Empty;

        // Thuộc tính liên kết
        public virtual Account? Account { get; set; }
        public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}