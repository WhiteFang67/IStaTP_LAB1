using System;
using System.Collections.Generic;

namespace OnlineStoreDomain.Model;

public partial class Review : Entity
{
    public int CustomerId { get; set; }

    public int ProductId { get; set; }

    public string Text { get; set; } = null!;

    public DateOnly Date { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
