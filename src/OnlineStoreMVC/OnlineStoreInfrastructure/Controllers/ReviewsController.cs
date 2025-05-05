using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineStoreInfrastructure;
using OnlineStoreDomain.Model;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace OnlineStoreInfrastructure.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly OnlineStoreContext _context;
        private readonly IdentityContext _identityContext;

        public ReviewsController(OnlineStoreContext context, IdentityContext identityContext)
        {
            _context = context;
            _identityContext = identityContext;
        }

        // Додавання відгуку (POST)
        [HttpPost]
        [Authorize(Roles = "user")]
        public IActionResult Create(int productId, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                TempData["ErrorMessage"] = "Поле коментаря є обов’язковим.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            // Отримання даних користувача
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

            // Отримання FirstName і LastName із IdentityContext
            var user = _identityContext.Users.FirstOrDefault(u => u.Id == userId);
            string userName = user != null && !string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(user.LastName)
                ? $"{user.FirstName} {user.LastName}"
                : User.Identity.Name; // Резервний варіант

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userEmail))
            {
                TempData["ErrorMessage"] = "Не вдалося отримати дані користувача. Перевірте ваш профіль.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            var review = new Review
            {
                UserId = userId,
                ProductId = productId,
                UserName = userName,
                UserEmail = userEmail,
                Text = text,
                Date = DateOnly.FromDateTime(DateTime.Today)
            };

            _context.Reviews.Add(review);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Відгук успішно додано!";
            return RedirectToAction("Details", "Products", new { id = productId });
        }

        // Видалення відгуку (POST)
        [HttpPost]
        public async Task<IActionResult> Delete(int id, int productId)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }

            // Перевірка прав: лише автор відгуку або адмін можуть видалити
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (review.UserId != userId && !User.IsInRole("admin"))
            {
                return Forbid();
            }

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Відгук успішно видалено!";
            return RedirectToAction("Details", "Products", new { id = productId });
        }
    }
}