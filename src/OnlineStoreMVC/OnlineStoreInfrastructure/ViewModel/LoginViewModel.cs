using System.ComponentModel.DataAnnotations;

namespace OnlineStoreInfrastructure.ViewModel
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Поле Електронна пошта обов'язкове")]
        [EmailAddress(ErrorMessage = "Невірний формат email")]
        [Display(Name = "Електронна пошта")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Поле Пароль обов'язкове")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; }

        [Display(Name = "Запам'ятати мене")]
        public bool RememberMe { get; set; }

        [Display(Name = "URL для повернення")]
        public string? ReturnUrl { get; set; } // Позначено як nullable
    }
}