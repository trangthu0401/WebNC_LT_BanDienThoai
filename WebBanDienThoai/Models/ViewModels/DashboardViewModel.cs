using System.Collections.Generic;

// Namespace của bạn có thể là WebBanDienThoai.ViewModels
namespace WebBanDienThoai.Models.ViewModels
{
    public class DashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int ProductCount { get; set; }
        public int UserCount { get; set; }
        public int OrderCount { get; set; }

        // Sửa lỗi: Khởi tạo danh sách
        public List<BestSellingProductViewModel> TopSellingProducts { get; set; } = new List<BestSellingProductViewModel>();
    }

    // Đảm bảo bạn cũng có class này
    
}