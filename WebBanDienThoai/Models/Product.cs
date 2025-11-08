using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebBanDienThoai.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string Name { get; set; } = null!;

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn một hãng sản xuất.")]
    public int BrandId { get; set; }

    public string? Chipset { get; set; }

    public string? OperatingSystem { get; set; }

    public short? BatteryCapacity { get; set; }

    public bool ChargerIncluded { get; set; }

    public decimal? ScreenSize { get; set; }

    public string? ScreenTech { get; set; }

    public short? RefreshRate { get; set; }

    public string? RearCamera { get; set; }

    public string? FrontCamera { get; set; }

    public decimal? Weight { get; set; }

    public string? Dimensions { get; set; }

    public string? Description { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public string? MainImage { get; set; }

    public bool IsActive { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public virtual Brand? Brand { get; set; } = null!;

    public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
}
