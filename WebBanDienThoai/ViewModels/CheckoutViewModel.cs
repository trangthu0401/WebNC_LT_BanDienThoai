using System.Collections.Generic;

namespace WebBanDienThoai.ViewModels
{
    public class CheckoutViewModel
    {
        // Thông tin khách hàng
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        // Thông tin địa chỉ
        public string Street { get; set; }
        public string District { get; set; }
        public string City { get; set; }
        public string Note { get; set; }

        // === THÊM THUỘC TÍNH NÀY ĐỂ SỬA LỖI ===
        // Để bắt giá trị (pickup/delivery) từ Trang 1
        public string? ShippingMethod { get; set; }
        // ====================================

        public string? PaymentMethod { get; set; }
        public string DiscountCode { get; set; }
        public decimal Discount { get; set; } = 0;

        // Giỏ hàng
        public List<CartItemViewModel> CartItems { get; set; } = new List<CartItemViewModel>();

        // Tổng tiền
        public decimal Subtotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal Total => Subtotal + ShippingFee - Discount;
    }
}