using System.ComponentModel.DataAnnotations;

namespace OnlineStoreDomain.Model;

public partial class ProductRating : Entity
{
    public int CustomerId { get; set; } // Обов’язкове поле, null не допускається

    public int ProductId { get; set; }

    [Range(0, 5, ErrorMessage = "Оцінка має бути від 0 до 5")]
    public float Rating { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}