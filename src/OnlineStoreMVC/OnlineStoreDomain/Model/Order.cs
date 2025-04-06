using System;
using System.Collections.Generic;

namespace OnlineStoreDomain.Model;

public partial class Order : Entity
{
    public int CustomerId { get; set; }

    public int StatusTypeId { get; set; }

    public int DeliveryServiceId { get; set; }

    public DateTime RegistrationDate { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual DeliveryService DeliveryService { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual StatuseType StatusType { get; set; } = null!;
}
