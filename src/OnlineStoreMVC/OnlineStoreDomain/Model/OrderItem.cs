using System;
using System.Collections.Generic;

namespace OnlineStoreDomain.Model;

public partial class OrderItem : Entity
{
    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public short Quantity { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
