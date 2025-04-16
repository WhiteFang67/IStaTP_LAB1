namespace OnlineStoreDomain.Model
{
    public class DeliveryService : Entity
    {
        public string Name { get; set; }
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<DeliveryDepartment> DeliveryDepartments { get; set; } = new List<DeliveryDepartment>();
    }
}