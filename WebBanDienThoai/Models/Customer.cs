namespace WebBanDienThoai.Models
{
    public class Customer
    {
        public int CustomerID { get; set; }
        public int AccountID { get; set; }
        public string FullName { get; set; }
        public string? Phone { get; set; } // Dùng ? để cho phép NULL
        public string Gender { get; set; }
        public DateTime? BirthDate { get; set; } // Dùng ? để cho phép NULL
        public string CustomerType { get; set; }

        // (Tùy chọn) Thêm thuộc tính điều hướng để liên kết với Account
        public virtual Account Account { get; set; }
    }
}
