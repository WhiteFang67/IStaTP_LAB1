using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineStoreInfrastructure;
using OnlineStoreDomain.Model;
using Microsoft.AspNetCore.Authorization;

namespace OnlineStoreInfrastructure.Controllers
{
    public class CartController : Controller
    {
        private readonly OnlineStoreContext _context;

        public CartController(OnlineStoreContext context)
        {
            _context = context;
        }

        // Перегляд кошика
        public IActionResult Index()
        {
            var cartItems = new List<OrderItem>();

            if (User.Identity.IsAuthenticated)
            {
                cartItems = _context.OrderItems
                    .Include(oi => oi.Product)
                    .Where(oi => oi.OrderId == null)
                    .ToList();
            }

            return View(cartItems);
        }

        // Додавання товару до кошика
        [HttpPost]
        [Authorize(Roles = "User")]
        public IActionResult AddToCart(int productId, int quantity, string returnUrl)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null)
            {
                return NotFound();
            }

            // Перевірка наявної кількості
            if (quantity > product.Quantity)
            {
                TempData["ErrorMessage"] = $"Неможливо додати {quantity} одиниць товару {product.Name}. Доступно лише {product.Quantity}.";
                return Redirect(returnUrl ?? Url.Action("Index", "Products", new { all = true }));
            }

            var cartItem = _context.OrderItems
                .FirstOrDefault(oi => oi.ProductId == productId && oi.OrderId == null);
            if (cartItem != null)
            {
                // Перевірка, чи нова кількість не перевищить доступну
                if (cartItem.Quantity + quantity > product.Quantity)
                {
                    TempData["ErrorMessage"] = $"Неможливо додати ще {quantity} одиниць товару {product.Name}. Доступно лише {product.Quantity - cartItem.Quantity}.";
                    return Redirect(returnUrl ?? Url.Action("Index", "Products", new { all = true }));
                }
                cartItem.Quantity += quantity;
                cartItem.TotalPrice = cartItem.Quantity * product.Price; // Оновлення TotalPrice
            }
            else
            {
                cartItem = new OrderItem
                {
                    ProductId = productId,
                    Quantity = quantity,
                    TotalPrice = quantity * product.Price, // Встановлення TotalPrice
                    OrderId = null
                };
                _context.OrderItems.Add(cartItem);
            }

            _context.SaveChanges();
            return Redirect(returnUrl ?? Url.Action("Index", "Products", new { all = true }));
        }

        // Видалення товару з кошика
        [HttpPost]
        [Authorize(Roles = "User")]
        public IActionResult RemoveFromCart(int id)
        {
            var cartItem = _context.OrderItems
                .FirstOrDefault(oi => oi.Id == id && oi.OrderId == null);
            if (cartItem == null)
            {
                return NotFound();
            }

            _context.OrderItems.Remove(cartItem);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}