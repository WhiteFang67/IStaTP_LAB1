using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineStoreInfrastructure;
using OnlineStoreDomain.Model;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace OnlineStoreInfrastructure.Controllers
{
    public class CartController : Controller
    {
        private readonly OnlineStoreContext _context;

        public CartController(OnlineStoreContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var cartItems = new List<OrderItem>();

            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                cartItems = _context.OrderItems
                    .Include(oi => oi.Product)
                    .Where(oi => oi.OrderId == null && oi.UserId == userId)
                    .ToList();
            }

            return View(cartItems);
        }

        [HttpPost]
        [Authorize(Roles = "user")]
        public IActionResult AddToCart(int productId, int quantity, string returnUrl)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null)
            {
                return NotFound();
            }

            if (quantity > product.Quantity)
            {
                TempData["ErrorMessage"] = $"Неможливо додати {quantity} одиниць товару {product.Name}. Доступно лише {product.Quantity}.";
                return Redirect(returnUrl ?? Url.Action("Index", "Products", new { all = true }));
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var cartItem = _context.OrderItems
                .FirstOrDefault(oi => oi.ProductId == productId && oi.OrderId == null && oi.UserId == userId);
            if (cartItem != null)
            {
                if (cartItem.Quantity + quantity > product.Quantity)
                {
                    TempData["ErrorMessage"] = $"Неможливо додати ще {quantity} одиниць товару {product.Name}. Доступно лише {product.Quantity - cartItem.Quantity}.";
                    return Redirect(returnUrl ?? Url.Action("Index", "Products", new { all = true }));
                }
                cartItem.Quantity += quantity;
                cartItem.TotalPrice = cartItem.Quantity * product.Price;
            }
            else
            {
                cartItem = new OrderItem
                {
                    ProductId = productId,
                    UserId = userId,
                    Quantity = quantity,
                    TotalPrice = quantity * product.Price,
                    OrderId = null
                };
                _context.OrderItems.Add(cartItem);
            }

            _context.SaveChanges();
            return Redirect(returnUrl ?? Url.Action("Index", "Products", new { all = true }));
        }

        [HttpPost]
        [Authorize(Roles = "user")]
        public IActionResult RemoveFromCart(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var cartItem = _context.OrderItems
                .FirstOrDefault(oi => oi.Id == id && oi.OrderId == null && oi.UserId == userId);
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