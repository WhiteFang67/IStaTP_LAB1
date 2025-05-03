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
        public int BirthYear { get; set; }

        [Required(ErrorMessage = "Поле Місяць народження обов'язкове")]
        [Range(1, 12, ErrorMessage = "Місяць має бути між 1 і 12")]
        [Display(Name = "Місяць народження")]
        public int BirthMonth { get; set; }

        [Required(ErrorMessage = "Поле День народження обов'язкове")]
        [Range(1, 31, ErrorMessage = "День має бути між 1 і 31")]
        [Display(Name = "День народження")]
        public int BirthDay { get; set; }

        [Required(ErrorMessage = "Поле Ім’я обов’язкове")]
        [StringLength(50, ErrorMessage = "Ім’я не може перевищувати 50 символів")]
        [Display(Name = "Ім’я")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Поле Прізвище обов’язкове")]
        [StringLength(50, ErrorMessage = "Прізвище не може перевищувати 50 символів")]
        [Display(Name = "Прізвище")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Поле Телефон обов’язкове")]
        [Phone(ErrorMessage = "Невірний формат телефону")]
        [Display(Name = "Телефон")]
        public string PhoneNumber { get; set; }
    }
}