using System;
using System.Collections.Generic;

namespace OnlineStoreDomain.Model;

public partial class StatusType : Entity
{
    public string Name { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
