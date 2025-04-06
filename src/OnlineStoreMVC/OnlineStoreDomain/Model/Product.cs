using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineStoreDomain.Model;

public partial class Product : Entity
{
    [Required(ErrorMessage = "Категорія є обов’язковою")]
    [Display(Name = "Категорія")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Назва продукту обов'язкова")]
    [Display(Name = "Назва")]
    public string Name { get; set; } = null!;

    [Display(Name = "Огляд")]
    public string? GeneralInfo { get; set; }

    [Display(Name = "Характеристики")]
    public string? Characteristics { get; set; }

    [Range(1, 10, ErrorMessage = "Рейтинг має бути від 1 до 10")]
    [Display(Name = "Рейтинг")]
    public short? Ratings { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Кількість не може бути від’ємною")]
    [Display(Name = "Кількість")]
    public int Quantity { get; set; }

    [Display(Name = "Категорія")]
    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}