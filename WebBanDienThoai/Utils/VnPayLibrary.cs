using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

// ĐẢM BẢO NAMESPACE KHỚP VỚI LỖI (WebBanDienThoai.Utils)
namespace WebBanDienThoai.Utils
{
    // Lớp helper này chứa logic để tạo chữ ký bảo mật cho VnPay
    public static class VnPayLibrary
    {
        // Hàm tạo chữ ký HMAC-SHA512
        public static string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString();
        }

        // Hàm sắp xếp và nối chuỗi dữ liệu
        public static string GetRequestDataQueryString(SortedList<string, string> data)
        {
            StringBuilder dataBuilder = new StringBuilder();
            foreach (KeyValuePair<string, string> kvp in data)
            {
                if (!String.IsNullOrEmpty(kvp.Value))
                {
                    dataBuilder.Append(Uri.EscapeDataString(kvp.Key) + "=" + Uri.EscapeDataString(kvp.Value) + "&");
                }
            }
            // Xóa dấu '&' cuối cùng
            if (dataBuilder.Length > 0)
            {
                dataBuilder.Remove(dataBuilder.Length - 1, 1);
            }
            return dataBuilder.ToString();
        }
    }
}