using System.ComponentModel.DataAnnotations;

namespace OnlineStoreDomain.Model
{
    public partial class ProductRating : Entity
    {
        [Required(ErrorMessage = "Користувач є обов’язковим")]
        public string UserId { get; set; }
        public int ProductId { get; set; }
        [Range(0, 5, ErrorMessage = "Оцінка має бути від 0 до 5")]
        public float Rating { get; set; }
        public virtual Product Product { get; set; } = null!;
    }
}