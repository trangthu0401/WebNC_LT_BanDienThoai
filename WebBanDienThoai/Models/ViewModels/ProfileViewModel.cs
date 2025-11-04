using System;
using System.ComponentModel.DataAnnotations;

namespace WebBanDienThoai.ViewModels
{
    public class ProfileViewModel
    {
        [Display(Name = "Email (không thể thay đổi)")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giới tính")]
        [Display(Name = "Giới tính")]
        public string Gender { get; set; } = string.Empty;

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        // Sửa: Khớp với CSDL (dùng DateTime?)
        public DateTime? BirthDate { get; set; }
    }
}