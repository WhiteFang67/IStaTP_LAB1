using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineStoreDomain.Model
{
    public class Order : Entity
    {
        public int? CustomerId { get; set; }

        [Required(ErrorMessage = "Ім’я є обов’язковим.")]
        [Display(Name = "Ім’я")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Прізвище є обов’язковим.")]
        [Display(Name = "Прізвище")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Електронна пошта є обов’язковою.")]
        [EmailAddress(ErrorMessage = "Некоректна електронна пошта.")]
        [Display(Name = "Електронна пошта")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Телефон є обов’язковим.")]
        [Display(Name = "Телефон")]
        public string Phone { get; set; }

        [Display(Name = "Ціна замовлення")]
        public decimal OrderPrice { get; set; }

        [Display(Name = "Статус")]
        public int StatusTypeId { get; set; }

        [Display(Name = "Служба доставки")]
        public int DeliveryServiceId { get; set; }

        [Display(Name = "Відділення")]
        public int DeliveryDepartmentId { get; set; }

        [Display(Name = "Дата реєстрації")]
        public DateTime RegistrationDate { get; set; }

        [Required(ErrorMessage = "Дата доставки є обов’язковою.")]
        [Display(Name = "Дата доставки")]
        public DateTime DeliveryDate { get; set; }

        public Customer Customer { get; set; }
        public StatusType StatusType { get; set; }
        public DeliveryService DeliveryService { get; set; }
        public DeliveryDepartment DeliveryDepartment { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}