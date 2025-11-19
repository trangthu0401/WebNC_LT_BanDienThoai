using System;
using System.Collections.Generic;

namespace WebBanDienThoai.Models;

public partial class CartItem
{
    public int CartItemId { get; set; }

    public int? CustomerId { get; set; }

    public int? VariantId { get; set; }

    public int Quantity { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ProductVariant? Variant { get; set; }
}
