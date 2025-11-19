using System;
using System.Collections.Generic;

namespace WebBanDienThoai.Models;

public partial class FavoriteDetail
{
    public int FavoriteDetailId { get; set; }

    public int? FavoriteId { get; set; }

    public int? VariantId { get; set; }

    public virtual Favorite? Favorite { get; set; }

    public virtual ProductVariant? Variant { get; set; }
}
