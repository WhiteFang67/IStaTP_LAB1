namespace OnlineStoreDomain.Model
{
    public class Order
    {
        public int Id { get; set; }
        public int? CustomerId { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public decimal OrderPrice { get; set; }
        public int StatusTypeId { get; set; }
        public int DeliveryServiceId { get; set; }
        public int DeliveryDepartmentId { get; set; }
        public DateTime RegistrationDate { get; set; }

        public Customer Customer { get; set; }
        public StatusType StatusType { get; set; }
        public DeliveryService DeliveryService { get; set; }
        public DeliveryDepartment DeliveryDepartment { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}