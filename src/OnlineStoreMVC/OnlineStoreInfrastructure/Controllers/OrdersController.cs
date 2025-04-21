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

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Відкрито сторінку списку замовлень");

            var orders = await _context.Orders
                .Include(o => o.DeliveryService)
                .Include(o => o.DeliveryDepartment)
                .Include(o => o.StatusType)
                .ToListAsync();

            return View(orders);
        }

        // GET: Orders/Details/5
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

            _logger.LogInformation("Відкрито деталі замовлення з Id {OrderId}", id);
            return View(order);
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

            var order = new Order
            {
                DeliveryDate = DateTime.Today.AddDays(1) // За замовчуванням завтра
            };

            return View(order);
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,LastName,Email,Phone,DeliveryServiceId,DeliveryDepartmentId,DeliveryDate")] Order order, string deliveryTime)
        {
            _logger.LogInformation("Отримано запит на створення замовлення");

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

            ModelState.Clear();

            if (string.IsNullOrEmpty(order.Name))
                ModelState.AddModelError("Name", "Ім’я обов’язкове.");
            if (string.IsNullOrEmpty(order.LastName))
                ModelState.AddModelError("LastName", "Прізвище обов’язкове.");
            if (string.IsNullOrEmpty(order.Email) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(order.Email))
                ModelState.AddModelError("Email", "Вкажіть коректну електронну пошту.");
            if (string.IsNullOrEmpty(order.Phone))
                ModelState.AddModelError("Phone", "Телефон обов’язковий.");
            if (order.DeliveryServiceId <= 0 || !_context.DeliveryServices.Any(ds => ds.Id == order.DeliveryServiceId))
                ModelState.AddModelError("DeliveryServiceId", "Оберіть службу доставки.");
            if (order.DeliveryDepartmentId <= 0 || !_context.DeliveryDepartments.Any(dd => dd.Id == order.DeliveryDepartmentId))
                ModelState.AddModelError("DeliveryDepartmentId", "Оберіть відділення доставки.");
            if (order.DeliveryDate < DateTime.Today.AddDays(1))
                ModelState.AddModelError("DeliveryDate", "Дата доставки має бути не раніше ніж через добу.");
            if (string.IsNullOrEmpty(deliveryTime) || !new[] { "08:00", "14:00", "18:00" }.Contains(deliveryTime))
                ModelState.AddModelError("deliveryTime", "Оберіть коректний час доставки.");

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
                    deliveryTime
                );
                ViewBag.CartItems = cartItems;
                ViewBag.TotalPrice = cartItems.Sum(oi => oi.TotalPrice);
                _logger.LogWarning("Помилка валідації: {Errors}", string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return View(order);
            }

            try
            {
                var timeParts = deliveryTime.Split(':');
                var deliveryTimeSpan = new TimeSpan(int.Parse(timeParts[0]), int.Parse(timeParts[1]), 0);
                order.DeliveryDate = order.DeliveryDate.Date + deliveryTimeSpan;
                var localTimeZone = TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time");
                order.RegistrationDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, localTimeZone);
                order.StatusTypeId = 1;
                order.OrderPrice = cartItems.Sum(oi => oi.TotalPrice);

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Замовлення створено з Id: {OrderId}", order.Id);

                foreach (var item in cartItems)
                    item.OrderId = order.Id;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Елементи кошика прив’язано до замовлення {OrderId}", order.Id);

                TempData["SuccessMessage"] = "Замовлення успішно створено!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при створенні замовлення");
                ModelState.AddModelError("", $"Помилка при створенні замовлення: {ex.Message}");
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
                    deliveryTime
                );
                ViewBag.CartItems = cartItems;
                ViewBag.TotalPrice = cartItems.Sum(oi => oi.TotalPrice);
                return View(order);
            }
        }

        // GET: Orders/Edit/5
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
                order.DeliveryDate.ToString("HH:mm"));
            ViewData["StatusTypeId"] = new SelectList(_context.StatusTypes, "Id", "Name", order.StatusTypeId);

            _logger.LogInformation("Відкрито сторінку редагування замовлення з Id {OrderId}", id);
            return View(order);
        }

        // POST: Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Email,Phone,DeliveryServiceId,DeliveryDepartmentId,DeliveryDate,StatusTypeId")] Order order, string deliveryTime)
        {
            if (id != order.Id)
            {
                _logger.LogWarning("Невідповідність Id при редагуванні замовлення: {Id} != {ModelId}", id, order.Id);
                return NotFound();
            }

            ModelState.Clear();

            if (string.IsNullOrEmpty(order.Email) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(order.Email))
                ModelState.AddModelError("Email", "Вкажіть коректну електронну пошту.");
            if (string.IsNullOrEmpty(order.Phone))
                ModelState.AddModelError("Phone", "Телефон обов’язковий.");
            if (order.DeliveryServiceId <= 0 || !_context.DeliveryServices.Any(ds => ds.Id == order.DeliveryServiceId))
                ModelState.AddModelError("DeliveryServiceId", "Оберіть службу доставки.");
            if (order.DeliveryDepartmentId <= 0 || !_context.DeliveryDepartments.Any(dd => dd.Id == order.DeliveryDepartmentId))
                ModelState.AddModelError("DeliveryDepartmentId", "Оберіть відділення доставки.");
            if (order.DeliveryDate < DateTime.Today.AddDays(1))
                ModelState.AddModelError("DeliveryDate", "Дата доставки має бути не раніше ніж через добу.");
            if (string.IsNullOrEmpty(deliveryTime) || !new[] { "08:00", "14:00", "18:00" }.Contains(deliveryTime))
                ModelState.AddModelError("deliveryTime", "Оберіть коректний час доставки.");
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
                    deliveryTime);
                ViewData["StatusTypeId"] = new SelectList(_context.StatusTypes, "Id", "Name", order.StatusTypeId);
                _logger.LogWarning("Помилка валідації: {Errors}", string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return View(order);
            }

            try
            {
                var existingOrder = await _context.Orders.FindAsync(id);
                if (existingOrder == null)
                {
                    _logger.LogWarning("Замовлення з Id {OrderId} не знайдено при редагуванні", id);
                    return NotFound();
                }

                var timeParts = deliveryTime.Split(':');
                var deliveryTimeSpan = new TimeSpan(int.Parse(timeParts[0]), int.Parse(timeParts[1]), 0);
                existingOrder.Email = order.Email;
                existingOrder.Phone = order.Phone;
                existingOrder.DeliveryServiceId = order.DeliveryServiceId;
                existingOrder.DeliveryDepartmentId = order.DeliveryDepartmentId;
                existingOrder.DeliveryDate = order.DeliveryDate.Date + deliveryTimeSpan;
                existingOrder.StatusTypeId = order.StatusTypeId;

                _context.Update(existingOrder);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Замовлення з Id {OrderId} успішно відредаговано", id);

                TempData["SuccessMessage"] = "Замовлення успішно відредаговано!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
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
                    deliveryTime);
                ViewData["StatusTypeId"] = new SelectList(_context.StatusTypes, "Id", "Name", order.StatusTypeId);
                return View(order);
            }
        }

        // GET: Orders/Delete/5
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

            _logger.LogInformation("Відкрито сторінку підтвердження видалення замовлення з Id {OrderId}", id);
            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                {
                    _logger.LogWarning("Замовлення з Id {OrderId} не знайдено при видаленні", id);
                    return NotFound();
                }

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Замовлення з Id {OrderId} успішно видалено", id);

                TempData["SuccessMessage"] = "Замовлення успішно видалено!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при видаленні замовлення з Id {OrderId}", id);
                TempData["ErrorMessage"] = "Помилка при видаленні замовлення.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Orders/GetDeliveryDepartments
        [HttpGet]
        public IActionResult GetDeliveryDepartments(int deliveryServiceId)
        {
            var departments = _context.DeliveryDepartments
                .Where(dd => dd.DeliveryServiceId == deliveryServiceId)
                .Select(dd => new { dd.Id, dd.Name })
                .ToList();

            return Json(departments);
        }
    }
}