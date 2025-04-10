using System;

namespace OnlineStoreDomain.Model;

public partial class Review : Entity
{
    public int? CustomerId { get; set; } // Nullable для анонімів

    public int ProductId { get; set; }

    public string UserName { get; set; } = null!;

    public string UserEmail { get; set; } = null!;

    public string Text { get; set; } = null!;

    public DateOnly Date { get; set; }

    public virtual Customer? Customer { get; set; } // Nullable для анонімів

    public virtual Product Product { get; set; } = null!;
}