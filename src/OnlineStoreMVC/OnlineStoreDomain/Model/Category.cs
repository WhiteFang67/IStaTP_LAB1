using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineStoreDomain.Model;

public partial class Category : Entity
{
    [Required(ErrorMessage = "Поле не повинно бути порожнім")]
    [Display(Name = "Назва")]

    public string Name { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
