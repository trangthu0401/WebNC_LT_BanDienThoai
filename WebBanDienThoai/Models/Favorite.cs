using System;
using System.Collections.Generic;

namespace WebBanDienThoai.Models;

public partial class Favorite
{
    public int FavoriteId { get; set; }

    public int? CustomerId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<FavoriteDetail> FavoriteDetails { get; set; } = new List<FavoriteDetail>();
}
