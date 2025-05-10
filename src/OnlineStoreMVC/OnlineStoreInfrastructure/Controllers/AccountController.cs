using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineStoreDomain.Models;
using OnlineStoreInfrastructure.ViewModel;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace OnlineStoreInfrastructure.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Перевірка унікальності електронної пошти
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Користувач із такою електронною поштою вже існує.");
                    return View(model);
                }

                // Перевіряємо коректність дати народження
                try
                {
                    var birthDate = new DateOnly(model.BirthYear, model.BirthMonth, model.BirthDay);
                    if (birthDate > DateOnly.FromDateTime(DateTime.Today))
                    {
                        ModelState.AddModelError(string.Empty, "Дата народження не може бути в майбутньому");
                        return View(model);
                    }

                    var user = new User
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        BirthDate = birthDate,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        PhoneNumber = "+38" + model.PhoneNumber // Додаємо префікс +38 до номера
                    };

                    var result = await _userManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, "user");
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return RedirectToAction("Index", "Home");
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                catch (ArgumentException)
                {
                    ModelState.AddModelError(string.Empty, "Некоректна дата народження");
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Неправильний email або пароль");
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }

    public class AlphabetValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            var input = value.ToString();
            if (string.IsNullOrWhiteSpace(input))
            {
                return new ValidationResult("Поле є обов'язковим.");
            }

            // Регулярний вираз: дозволяємо літери будь-якої мови (\p{L}), пробіли, апостроф і дефіс,
            // виключаємо цифри та інші символи
            var regex = new Regex(@"^[\p{L} '\-]+$", RegexOptions.None);
            if (!regex.IsMatch(input))
            {
                return new ValidationResult("Поле може містити лише літери, пробіл, апостроф або дефіс.");
            }

            return ValidationResult.Success;
        }
    }

    public class DigitsOnlyValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            var input = value.ToString();
            if (string.IsNullOrWhiteSpace(input))
            {
                return new ValidationResult("Поле є обов'язковим.");
            }

            // Регулярний вираз: дозволяємо лише цифри
            var regex = new Regex(@"^\d+$", RegexOptions.None);
            if (!regex.IsMatch(input))
            {
                return new ValidationResult("Номер телефону може містити лише цифри.");
            }

            return ValidationResult.Success;
        }
    }

    public class ValidDayAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            var model = (RegisterViewModel)validationContext.ObjectInstance;
            int day = (int)value;
            int month = model.BirthMonth;
            int year = model.BirthYear;

            if (month < 1 || month > 12 || year < 1900 || year > 2025 || day < 1)
            {
                return new ValidationResult("Некоректна дата народження.");
            }

            int[] daysInMonth = { 0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

            // Перевірка на високосний рік для лютого
            if (month == 2 && IsLeapYear(year))
            {
                if (day > 29)
                {
                    return new ValidationResult("У високосному році лютий має максимум 29 днів.");
                }
            }
            else if (day > daysInMonth[month])
            {
                return new ValidationResult($"Місяць {month} має максимум {daysInMonth[month]} днів.");
            }

            return ValidationResult.Success;
        }

        private bool IsLeapYear(int year)
        {
            return year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
        }
    }
}