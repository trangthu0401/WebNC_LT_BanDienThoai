using System;
using System.Collections.Generic;

namespace WebBanDienThoai.Models;

public partial class ReviewDetail
{
    public int ReviewDetailId { get; set; }

    public int? ReviewId { get; set; }

    public int? VariantId { get; set; }

    public int? Rating { get; set; }

    public string? Comment { get; set; }

    public virtual Review? Review { get; set; }

    public virtual ProductVariant? Variant { get; set; }
}
