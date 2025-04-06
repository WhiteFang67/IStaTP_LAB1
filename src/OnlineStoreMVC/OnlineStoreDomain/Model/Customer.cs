using System;
using System.Collections.Generic;

namespace OnlineStoreDomain.Model;

public partial class Customer : Entity
{
    public DateOnly? BirthDate { get; set; }

    public string? Email { get; set; }

    public string PhoneNumber { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
