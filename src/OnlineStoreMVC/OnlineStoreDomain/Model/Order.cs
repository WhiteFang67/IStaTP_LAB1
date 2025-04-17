using System.ComponentModel.DataAnnotations;

namespace OnlineStoreDomain.Model
{
    public class Order
    {
        public int Id { get; set; }
        public int? CustomerId { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string Phone { get; set; }
        public decimal OrderPrice { get; set; }
        public int StatusTypeId { get; set; }
        public int DeliveryServiceId { get; set; }
        public int DeliveryDepartmentId { get; set; }
        public DateTime RegistrationDate { get; set; }
        [Required]
        public DateTime DeliveryDate { get; set; }

        public Customer Customer { get; set; }
        public StatusType StatusType { get; set; }
        public DeliveryService DeliveryService { get; set; }
        public DeliveryDepartment DeliveryDepartment { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}