using System;
using System.Collections.Generic;

namespace OnlineStoreDomain.Model;

public partial class DeliveryService : Entity
{
    public string Departmets { get; set; } = null!;

    public string Name { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
