﻿namespace OnlineStoreDomain.Model;

public partial class OrderItem : Entity
{
    public int? OrderId { get; set; } 
    public int ProductId { get; set; }
    public int Quantity { get; set; } 

    public virtual Order? Order { get; set; }
    public virtual Product Product { get; set; } = null!;
}