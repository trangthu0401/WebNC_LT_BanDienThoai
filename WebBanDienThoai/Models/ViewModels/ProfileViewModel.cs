using System.ComponentModel.DataAnnotations;

namespace WebBanDienThoai.ViewModels
{
    public class ProfileViewModel
    {
        // Hiển thị, không cho sửa
        [Display(Name = "Email (Không thể thay đổi)")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và tên")]
        [StringLength(100)]
        public string FullName { get; set; }

        [Display(Name = "Số điện thoại")]
        [DataType(DataType.PhoneNumber)]
        [StringLength(15)]
        public string? Phone { get; set; }

        [Required]
        [Display(Name = "Giới tính")]
        public string Gender { get; set; }

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }
    }
}
