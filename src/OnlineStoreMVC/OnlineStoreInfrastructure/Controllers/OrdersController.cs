﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineStoreDomain.Model;
using OnlineStoreInfrastructure;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace OnlineStoreInfrastructure.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly OnlineStoreContext _context;
        private readonly IdentityContext _identityContext;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(OnlineStoreContext context, IdentityContext identityContext, ILogger<OrdersController> logger)
        {
            _context = context;
            _identityContext = identityContext;
            _logger = logger;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Відкрито сторінку списку замовлень");

            var orders = new List<Order>();

            if (User.Identity.IsAuthenticated)
            {
                var ordersQuery = _context.Orders
                    .Include(o => o.DeliveryService)
                    .Include(o => o.DeliveryDepartment)
                    .Include(o => o.StatusType)
                    .AsQueryable();

                if (!User.IsInRole("admin"))
                {
                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    ordersQuery = ordersQuery.Where(o => o.UserId == userId)
                                            .OrderBy(o => o.UserId);
                }

                orders = await ordersQuery.ToListAsync();
            }

            return View(orders);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Спроба переглянути деталі замовлення без Id");
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.DeliveryService)
                .Include(o => o.DeliveryDepartment)
                .Include(o => o.StatusType)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                _logger.LogWarning("Замовлення з Id {OrderId} не знайдено", id);
                return NotFound();
            }

            if (!User.IsInRole("admin") && order.UserId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
            {
                _logger.LogWarning("Користувач {UserId} намагався переглянути чуже замовлення з Id {OrderId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);
                return Forbid();
            }

            _logger.LogInformation("Відкрито деталі замовлення з Id {OrderId}", id);
            return View(order);
        }

        [Authorize(Roles = "user")]
        public IActionResult Create()
        {
            _logger.LogInformation("Відкрито сторінку оформлення замовлення");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var cartItems = _context.OrderItems
                .Include(oi => oi.Product)
                .Where(oi => oi.OrderId == null && oi.UserId == userId)
                .ToList();

            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Ваш кошик порожній. Додайте товари перед оформленням замовлення.";
                _logger.LogWarning("Спроба оформити замовлення з порожнім кошиком");
                return RedirectToAction("Index", "Cart");
            }

            var user = _identityContext.Users.FirstOrDefault(u => u.Id == userId);
            var order = new Order
            {
                DeliveryDate = DateTime.Today.AddDays(1),
                DeliveryTime = "08:00",
                Email = user?.Email ?? User.Identity.Name,
                Name = user?.FirstName ?? "",
                LastName = user?.LastName ?? "",
                Phone = user?.PhoneNumber ?? ""
            };

            ViewData["DeliveryServiceId"] = new SelectList(_context.DeliveryServices, "Id", "Name");
            ViewData["DeliveryDepartmentId"] = new SelectList(Enumerable.Empty<SelectListItem>());
            ViewData["DeliveryTime"] = new SelectList(
                new[]
                {
                    new { Value = "08:00", Text = "08:00" },
                    new { Value = "14:00", Text = "14:00" },
                    new { Value = "18:00", Text = "18:00" }
                },
                "Value",
                "Text"
            );
            ViewBag.CartItems = cartItems;
            ViewBag.TotalPrice = cartItems.Sum(oi => oi.TotalPrice);

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> Create([Bind("Name,LastName,Email,Phone,DeliveryServiceId,DeliveryDepartmentId,DeliveryDate,DeliveryTime")] Order order)
        {
            _logger.LogInformation("Отримано запит на створення замовлення");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var cartItems = _context.OrderItems
                .Include(oi => oi.Product)
                .Where(oi => oi.OrderId == null && oi.UserId == userId)
                .ToList();

            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Ваш кошик порожній. Додайте товари перед оформленням замовлення.";
                _logger.LogWarning("Спроба оформити замовлення з порожнім кошиком");
                return RedirectToAction("Index", "Cart");
            }

            ModelState.Clear();

            if (string.IsNullOrEmpty(order.Name))
                ModelState.AddModelError("Name", "Ім’я є обов’язковим.");
            if (string.IsNullOrEmpty(order.LastName))
                ModelState.AddModelError("LastName", "Прізвище є обов’язковим.");
            if (string.IsNullOrEmpty(order.Email) || !new EmailAddressAttribute().IsValid(order.Email))
                ModelState.AddModelError("Email", "Вкажіть коректну електронну пошту.");
            if (string.IsNullOrEmpty(order.Phone))
                ModelState.AddModelError("Phone", "Телефон є обов’язковим.");
            if (order.DeliveryServiceId <= 0 || !_context.DeliveryServices.Any(ds => ds.Id == order.DeliveryServiceId))
                ModelState.AddModelError("DeliveryServiceId", "Оберіть службу доставки.");
            if (order.DeliveryDepartmentId <= 0 || !_context.DeliveryDepartments.Any(dd => dd.Id == order.DeliveryDepartmentId))
                ModelState.AddModelError("DeliveryDepartmentId", "Оберіть відділення доставки.");
            if (order.DeliveryDate < DateTime.Today.AddDays(1))
                ModelState.AddModelError("DeliveryDate", "Дата доставки має бути не раніше ніж через добу.");
            if (string.IsNullOrEmpty(order.DeliveryTime) || !new[] { "08:00", "14:00", "18:00" }.Contains(order.DeliveryTime))
                ModelState.AddModelError("DeliveryTime", "Час доставки є обов’язковим і має бути 08:00, 14:00 або 18:00.");

            foreach (var item in cartItems)
            {
                if (item.Product == null)
                {
                    ModelState.AddModelError("", $"Товар з Id {item.ProductId} не знайдено.");
                    _logger.LogWarning("Товар з Id {ProductId} не знайдено в кошику", item.ProductId);
                }
                else if (item.Quantity > item.Product.Quantity)
                {
                    ModelState.AddModelError("", $"Недостатньо товару {item.Product.Name}. Доступно: {item.Product.Quantity}, потрібно: {item.Quantity}.");
                    _logger.LogWarning("Недостатньо товару {ProductName}: доступно {Available}, потрібно {Requested}", item.Product.Name, item.Product.Quantity, item.Quantity);
                }
            }

            if (!ModelState.IsValid)
            {
                ViewData["DeliveryServiceId"] = new SelectList(_context.DeliveryServices, "Id", "Name", order.DeliveryServiceId);
                ViewData["DeliveryDepartmentId"] = new SelectList(
                    _context.DeliveryDepartments.Where(dd => dd.DeliveryServiceId == order.DeliveryServiceId),
                    "Id", "Name", order.DeliveryDepartmentId);
                ViewData["DeliveryTime"] = new SelectList(
                    new[]
                    {
                        new { Value = "08:00", Text = "08:00" },
                        new { Value = "14:00", Text = "14:00" },
                        new { Value = "18:00", Text = "18:00" }
                    },
                    "Value",
                    "Text",
                    order.DeliveryTime
                );
                ViewBag.CartItems = cartItems;
                ViewBag.TotalPrice = cartItems.Sum(oi => oi.TotalPrice);
                _logger.LogWarning("Помилка валідації: {Errors}", string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return View(order);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in cartItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.Quantity -= item.Quantity;
                        if (product.Quantity < 0)
                            throw new Exception($"Кількість товару {product.Name} стала від’ємною.");
                        _context.Update(product);
                    }
                }

                var localTimeZone = TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time");
                order.RegistrationDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, localTimeZone);
                order.StatusTypeId = 1;
                order.OrderPrice = cartItems.Sum(oi => oi.TotalPrice);
                order.UserId = userId;

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Замовлення створено з Id: {OrderId}", order.Id);

                foreach (var item in cartItems)
                    item.OrderId = order.Id;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Елементи кошика прив’язано до замовлення {OrderId}", order.Id);

                await transaction.CommitAsync();
                TempData["SuccessMessage"] = "Замовлення успішно створено!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Помилка при створенні замовлення");
                TempData["ErrorMessage"] = $"Помилка при створенні замовлення: {ex.Message}";
                ViewData["DeliveryServiceId"] = new SelectList(_context.DeliveryServices, "Id", "Name", order.DeliveryServiceId);
                ViewData["DeliveryDepartmentId"] = new SelectList(
                    _context.DeliveryDepartments.Where(dd => dd.DeliveryServiceId == order.DeliveryServiceId),
                    "Id", "Name", order.DeliveryDepartmentId);
                ViewData["DeliveryTime"] = new SelectList(
                    new[]
                    {
                        new { Value = "08:00", Text = "08:00" },
                        new { Value = "14:00", Text = "14:00" },
                        new { Value = "18:00", Text = "18:00" }
                    },
                    "Value",
                    "Text",
                    order.DeliveryTime
                );
                ViewBag.CartItems = cartItems;
                ViewBag.TotalPrice = cartItems.Sum(oi => oi.TotalPrice);
                return View(order);
            }
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Спроба редагувати замовлення без Id");
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.DeliveryService)
                .Include(o => o.DeliveryDepartment)
                .Include(o => o.StatusType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                _logger.LogWarning("Замовлення з Id {OrderId} не знайдено", id);
                return NotFound();
            }

            ViewData["DeliveryServiceId"] = new SelectList(_context.DeliveryServices, "Id", "Name", order.DeliveryServiceId);
            ViewData["DeliveryDepartmentId"] = new SelectList(
                _context.DeliveryDepartments.Where(dd => dd.DeliveryServiceId == order.DeliveryServiceId),
                "Id", "Name", order.DeliveryDepartmentId);
            ViewData["DeliveryTime"] = new SelectList(
                new[]
                {
                    new { Value = "08:00", Text = "08:00" },
                    new { Value = "14:00", Text = "14:00" },
                    new { Value = "18:00", Text = "18:00" }
                },
                "Value",
                "Text",
                order.DeliveryTime
            );
            ViewData["StatusTypeId"] = new SelectList(_context.StatusTypes, "Id", "Name", order.StatusTypeId);

            _logger.LogInformation("Відкрито сторінку редагування замовлення з Id {OrderId}", id);
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DeliveryServiceId,DeliveryDepartmentId,DeliveryDate,DeliveryTime,StatusTypeId")] Order order)
        {
            if (id != order.Id)
            {
                _logger.LogWarning("Невідповідність Id при редагуванні замовлення: {Id} != {ModelId}", id, order.Id);
                return NotFound();
            }

            ModelState.Clear();

            if (order.DeliveryServiceId <= 0 || !_context.DeliveryServices.Any(ds => ds.Id == order.DeliveryServiceId))
                ModelState.AddModelError("DeliveryServiceId", "Оберіть службу доставки.");
            if (order.DeliveryDepartmentId <= 0 || !_context.DeliveryDepartments.Any(dd => dd.Id == order.DeliveryDepartmentId))
                ModelState.AddModelError("DeliveryDepartmentId", "Оберіть відділення доставки.");
            if (order.DeliveryDate < DateTime.Today.AddDays(1))
                ModelState.AddModelError("DeliveryDate", "Дата доставки має бути не раніше ніж через добу.");
            if (string.IsNullOrEmpty(order.DeliveryTime) || !new[] { "08:00", "14:00", "18:00" }.Contains(order.DeliveryTime))
                ModelState.AddModelError("DeliveryTime", "Час доставки є обов’язковим і має бути 08:00, 14:00 або 18:00.");
            if (order.StatusTypeId <= 0 || !_context.StatusTypes.Any(st => st.Id == order.StatusTypeId))
                ModelState.AddModelError("StatusTypeId", "Оберіть статус замовлення.");

            if (!ModelState.IsValid)
            {
                ViewData["DeliveryServiceId"] = new SelectList(_context.DeliveryServices, "Id", "Name", order.DeliveryServiceId);
                ViewData["DeliveryDepartmentId"] = new SelectList(
                    _context.DeliveryDepartments.Where(dd => dd.DeliveryServiceId == order.DeliveryServiceId),
                    "Id", "Name", order.DeliveryDepartmentId);
                ViewData["DeliveryTime"] = new SelectList(
                    new[]
                    {
                        new { Value = "08:00", Text = "08:00" },
                        new { Value = "14:00", Text = "14:00" },
                        new { Value = "18:00", Text = "18:00" }
                    },
                    "Value",
                    "Text",
                    order.DeliveryTime
                );
                ViewData["StatusTypeId"] = new SelectList(_context.StatusTypes, "Id", "Name", order.StatusTypeId);
                _logger.LogWarning("Помилка валідації: {Errors}", string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return View(order);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingOrder = await _context.Orders.FindAsync(id);
                if (existingOrder == null)
                {
                    _logger.LogWarning("Замовлення з Id {OrderId} не знайдено при редагуванні", id);
                    return NotFound();
                }

                existingOrder.Email = existingOrder.Email; // Залишаємо незмінним
                existingOrder.Phone = existingOrder.Phone; // Залишаємо незмінним
                existingOrder.DeliveryServiceId = order.DeliveryServiceId;
                existingOrder.DeliveryDepartmentId = order.DeliveryDepartmentId;
                existingOrder.DeliveryDate = order.DeliveryDate.Date;
                existingOrder.DeliveryTime = order.DeliveryTime;
                existingOrder.StatusTypeId = order.StatusTypeId;

                _context.Update(existingOrder);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Замовлення з Id {OrderId} успішно відредаговано", id);

                await transaction.CommitAsync();
                TempData["SuccessMessage"] = "Замовлення успішно відредаговано!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Помилка при редагуванні замовлення з Id {OrderId}", id);
                ModelState.AddModelError("", $"Помилка при редагуванні замовлення: {ex.Message}");
                ViewData["DeliveryServiceId"] = new SelectList(_context.DeliveryServices, "Id", "Name", order.DeliveryServiceId);
                ViewData["DeliveryDepartmentId"] = new SelectList(
                    _context.DeliveryDepartments.Where(dd => dd.DeliveryServiceId == order.DeliveryServiceId),
                    "Id", "Name", order.DeliveryDepartmentId);
                ViewData["DeliveryTime"] = new SelectList(
                    new[]
                    {
                        new { Value = "08:00", Text = "08:00" },
                        new { Value = "14:00", Text = "14:00" },
                        new { Value = "18:00", Text = "18:00" }
                    },
                    "Value",
                    "Text",
                    order.DeliveryTime
                );
                ViewData["StatusTypeId"] = new SelectList(_context.StatusTypes, "Id", "Name", order.StatusTypeId);
                return View(order);
            }
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Спроба видалити замовлення без Id");
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.DeliveryService)
                .Include(o => o.DeliveryDepartment)
                .Include(o => o.StatusType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                _logger.LogWarning("Замовлення з Id {OrderId} не знайдено", id);
                return NotFound();
            }

            if (order.StatusTypeId != 4)
            {
                _logger.LogWarning("Спроба видалити замовлення з Id {OrderId} зі статусом {Status}", id, order.StatusType.Name);
                TempData["ErrorMessage"] = "Лише скасовані замовлення можна видалити.";
                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation("Відкрито сторінку підтвердження видалення замовлення з Id {OrderId}", id);
            return View(order);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.Orders
                    .Include(o => o.StatusType)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (order == null)
                {
                    _logger.LogWarning("Замовлення з Id {OrderId} не знайдено при видаленні", id);
                    return NotFound();
                }

                if (order.StatusTypeId != 4)
                {
                    _logger.LogWarning("Спроба видалити замовлення з Id {OrderId} зі статусом {Status}", id, order.StatusType.Name);
                    TempData["ErrorMessage"] = "Лише скасовані замовлення можна видалити.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Замовлення з Id {OrderId} успішно видалено", id);

                await transaction.CommitAsync();
                TempData["SuccessMessage"] = "Замовлення успішно видалено!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Помилка при видаленні замовлення з Id {OrderId}", id);
                TempData["ErrorMessage"] = "Помилка при видаленні замовлення: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetDeliveryDepartments(int deliveryServiceId)
        {
            var departments = _context.DeliveryDepartments
                .Where(dd => dd.DeliveryServiceId == deliveryServiceId)
                .Select(dd => new { dd.Id, dd.Name })
                .ToList();

            return Json(departments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Cancel(int id)
        {
            _logger.LogInformation("Отримано POST-запит на скасування замовлення з Id {OrderId}", id);

            var order = await _context.Orders
                .Include(o => o.StatusType)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                _logger.LogWarning("Замовлення з Id {OrderId} не знайдено", id);
                TempData["ErrorMessage"] = "Замовлення не знайдено.";
                return RedirectToAction(nameof(Index));
            }

            if (!User.IsInRole("admin") && order.UserId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
            {
                _logger.LogWarning("Користувач {UserId} намагався скасувати чуже замовлення з Id {OrderId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);
                return Forbid();
            }

            if (order.StatusTypeId != 1)
            {
                _logger.LogWarning("Спроба скасувати замовлення з Id {OrderId} зі статусом {Status}", id, order.StatusType?.Name ?? "невідомий");
                TempData["ErrorMessage"] = "Замовлення не можна скасувати, оскільки воно не в статусі 'В обробці'.";
                return RedirectToAction(nameof(Index));
            }

            if (!order.OrderItems.Any())
            {
                _logger.LogWarning("Замовлення з Id {OrderId} не містить елементів", id);
                TempData["ErrorMessage"] = "Замовлення не містить товарів.";
                return RedirectToAction(nameof(Index));
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in order.OrderItems)
                {
                    if (item.Product == null)
                    {
                        _logger.LogWarning("Товар з Id {ProductId} не знайдено для OrderItem {OrderItemId} у замовленні {OrderId}", item.ProductId, item.Id, id);
                        throw new Exception($"Товар з Id {item.ProductId} не знайдено.");
                    }

                    _logger.LogInformation("Оновлення кількості для товару {ProductName} (Id: {ProductId}): додаємо {Quantity}", item.Product.Name, item.ProductId, item.Quantity);
                    item.Product.Quantity += item.Quantity;
                    if (item.Product.Quantity < 0)
                    {
                        _logger.LogWarning("Кількість товару {ProductName} (Id: {ProductId}) стала від’ємною після додавання {Quantity}", item.Product.Name, item.ProductId, item.Quantity);
                        throw new Exception($"Кількість товару {item.Product.Name} стала від’ємною.");
                    }
                    _context.Update(item.Product);
                }

                order.StatusTypeId = 4;
                _context.Update(order);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Замовлення з Id {OrderId} успішно скасовано. Статус змінено на 'Скасоване'", id);

                await transaction.CommitAsync();
                _logger.LogInformation("Транзакцію для скасування замовлення {OrderId} успішно завершено", id);
                TempData["SuccessMessage"] = "Замовлення успішно скасовано!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Помилка при скасуванні замовлення з Id {OrderId}", id);
                TempData["ErrorMessage"] = $"Помилка при скасуванні замовлення: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}