using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineStoreInfrastructure;
using OnlineStoreDomain.Model;

namespace OnlineStoreInfrastructure.Controllers
{
    [Authorize] // Тільки авторизовані користувачі можуть оцінювати
    public class ProductRatingsController : Controller
    {
        private readonly OnlineStoreContext _context;

        public ProductRatingsController(OnlineStoreContext context)
        {
            _context = context;
        }

        [HttpPost]
        [AllowAnonymous] // Дозволяємо неавторизованим бачити помилку
        public async Task<IActionResult> Rate(int productId, float rating)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["RatingError"] = "Будь ласка, увійдіть, щоб оцінити товар.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            if (rating < 0 || rating > 5)
            {
                TempData["RatingError"] = "Оцінка має бути від 0 до 5.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var existingRating = await _context.ProductRatings
                .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);

            if (existingRating != null)
            {
                TempData["RatingError"] = "Ви вже оцінили цей товар.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            var productRating = new ProductRating
            {
                UserId = userId,
                ProductId = productId,
                Rating = rating
            };

            _context.ProductRatings.Add(productRating);
            await UpdateProductRating(productId);
            return RedirectToAction("Details", "Products", new { id = productId });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int productId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var rating = await _context.ProductRatings
                .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);

            if (rating == null)
            {
                TempData["RatingError"] = "Ви ще не оцінили цей товар.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            _context.ProductRatings.Remove(rating);
            await UpdateProductRating(productId);
            return RedirectToAction("Details", "Products", new { id = productId });
        }

        private async Task UpdateProductRating(int productId)
        {
            var product = await _context.Products
                .Include(p => p.ProductRatings)
                .FirstOrDefaultAsync(p => p.Id == productId);
            if (product != null)
            {
                product.Ratings = product.ProductRatings.Any()
                    ? (float?)Math.Round(product.ProductRatings.Average(r => r.Rating), 2)
                    : null;
                await _context.SaveChangesAsync();
            }
        }
    }
}