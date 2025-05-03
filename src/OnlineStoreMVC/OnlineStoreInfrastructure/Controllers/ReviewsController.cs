using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineStoreInfrastructure;
using OnlineStoreDomain.Model;
using Microsoft.AspNetCore.Authorization;

namespace OnlineStoreInfrastructure.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly OnlineStoreContext _context;

        public ReviewsController(OnlineStoreContext context)
        {
            _context = context;
        }

        // Додавання відгуку (POST)
        [HttpPost]
        [Authorize(Roles = "User")]
        public IActionResult Create(int productId, string userName, string userEmail, string text)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(userEmail) || string.IsNullOrWhiteSpace(text))
            {
                TempData["ErrorMessage"] = "Усі поля (ім’я, email, коментар) є обов’язковими.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            var review = new Review
            {
                UserId = null, // Для анонімів
                ProductId = productId,
                UserName = userName,
                UserEmail = userEmail,
                Text = text,
                Date = DateOnly.FromDateTime(DateTime.Today)
            };

            _context.Reviews.Add(review);
            _context.SaveChanges();

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

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Products", new { id = productId });
        }
    }
}