using System.ComponentModel.DataAnnotations;

namespace OnlineStoreInfrastructure.ViewModel
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Поле Email обов'язкове")]
        [EmailAddress(ErrorMessage = "Невірний формат email")]
        [Display(Name = "Електронна пошта")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Поле Пароль обов'язкове")]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "Пароль має містити щонайменше {2} символів.", MinimumLength = 6)]
        [Display(Name = "Пароль")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Підтвердження пароля")]
        [Compare("Password", ErrorMessage = "Пароль і підтвердження пароля не збігаються.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Поле Рік народження обов'язкове")]
        [Range(1900, 2025, ErrorMessage = "Рік має бути між 1900 і 2025")]
        [Display(Name = "Рік народження")]
        public int Year { get; set; }
    }
}