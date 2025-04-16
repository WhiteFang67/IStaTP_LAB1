using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineStoreDomain.Model
{
    public class DeliveryDepartment : Entity
    {
        public string Name { get; set; }
        public int DeliveryServiceId { get; set; }
        public DeliveryService DeliveryService { get; set; }
    }
}
