using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineStoreDomain.Model;
using OnlineStoreInfrastructure;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace OnlineStoreInfrastructure.Controllers
{
    public class OrdersController : Controller
    {
        private readonly OnlineStoreContext _context;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(OnlineStoreContext context, ILogger<OrdersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Orders/Create
        public IActionResult Create()
        {
            _logger.LogInformation("Відкрито сторінку оформлення замовлення");

            var cartItems = _context.OrderItems
                .Include(oi => oi.Product)
                .Where(oi => oi.OrderId == null)
                .ToList();

            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Ваш кошик порожній. Додайте товари перед оформленням замовлення.";
                _logger.LogWarning("Спроба оформити замовлення з порожнім кошиком");
                return RedirectToAction("Index", "Cart");
            }

            var model = new OrderCreateViewModel
            {
                CartItems = cartItems,
                TotalPrice = cartItems.Sum(oi => oi.TotalPrice),
                DeliveryServices = new SelectList(_context.DeliveryServices, "Id", "Name")
            };

            return View(model);
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            _logger.LogInformation("Отримано запит на створення замовлення");

            // Отримуємо товари з кошика
            var cartItems = _context.OrderItems
                .Include(oi => oi.Product)
                .Where(oi => oi.OrderId == null)
                .ToList();

            // Перевірка, чи кошик не порожній
            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Ваш кошик порожній. Додайте товари перед оформленням замовлення.";
                _logger.LogWarning("Спроба оформити замовлення з порожнім кошиком");
                return RedirectToAction("Index", "Cart");
            }

            // Видаляємо помилки валідації для полів, які заповнюються сервером
            ModelState.Remove("CartItems");
            ModelState.Remove("DeliveryServices");
            ModelState.Remove("TotalPrice");

            if (!ModelState.IsValid)
            {
                model.CartItems = cartItems;
                model.TotalPrice = cartItems.Sum(oi => oi.TotalPrice);
                model.DeliveryServices = new SelectList(_context.DeliveryServices, "Id", "Name", model.DeliveryServiceId);
                _logger.LogWarning("Помилка валідації моделі: {Errors}", string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return View(model);
            }

            try
            {
                // Створюємо замовлення
                var order = new Order
                {
                    CustomerId = null, // Для гостей
                    Name = model.Name,
                    LastName = model.LastName,
                    Email = model.Email,
                    Phone = model.Phone,
                    StatusTypeId = 1, // "В обробці"
                    DeliveryServiceId = model.DeliveryServiceId,
                    RegistrationDate = DateTime.UtcNow,
                    OrderPrice = cartItems.Sum(oi => oi.TotalPrice)
                };

                // Додаємо замовлення до контексту
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Замовлення створено з Id: {OrderId}", order.Id);

                // Оновлюємо OrderId для елементів кошика
                foreach (var item in cartItems)
                {
                    item.OrderId = order.Id;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Елементи кошика прив’язано до замовлення {OrderId}", order.Id);

                TempData["SuccessMessage"] = "Замовлення успішно створено!";
                return RedirectToAction("Index", "Products", new { all = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при створенні замовлення");
                ModelState.AddModelError("", $"Помилка при створенні замовлення: {ex.Message}");
                model.CartItems = cartItems;
                model.TotalPrice = cartItems.Sum(oi => oi.TotalPrice);
                model.DeliveryServices = new SelectList(_context.DeliveryServices, "Id", "Name", model.DeliveryServiceId);
                return View(model);
            }
        }
    }
        public class OrderCreateViewModel
        {
            [Required(ErrorMessage = "Ім’я обов’язкове.")]
            public string Name { get; set; }

            [Required(ErrorMessage = "Прізвище обов’язкове.")]
            public string LastName { get; set; }

            [Required(ErrorMessage = "Email обов’язковий.")]
            [EmailAddress(ErrorMessage = "Вкажіть коректний email.")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Телефон обов’язковий.")]
            public string Phone { get; set; }

            [Required(ErrorMessage = "Оберіть службу доставки.")]
            public int DeliveryServiceId { get; set; }

            [Required(ErrorMessage = "Оберіть відділення доставки.")]
            public int DeliveryDepartmentId { get; set; }

            public List<OrderItem> CartItems { get; set; }
            public decimal TotalPrice { get; set; }
            public SelectList DeliveryServices { get; set; }
            public SelectList DeliveryDepartments { get; set; }
        }
}