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
                .Select(o => new OrderViewModel
                {
                    Id = o.Id,
                    Name = o.Name,
                    LastName = o.LastName,
                    Email = o.Email,
                    Phone = o.Phone,
                    DeliveryServiceName = o.DeliveryService.Name,
                    DeliveryDepartmentName = o.DeliveryDepartment.Name,
                    StatusTypeName = o.StatusType.Name,
                    OrderPrice = o.OrderPrice,
                    RegistrationDate = o.RegistrationDate
                })
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

            var model = new OrderDetailsViewModel
            {
                Id = order.Id,
                Name = order.Name,
                LastName = order.LastName,
                Email = order.Email,
                Phone = order.Phone,
                DeliveryServiceName = order.DeliveryService.Name,
                DeliveryDepartmentName = order.DeliveryDepartment.Name,
                StatusTypeName = order.StatusType.Name,
                OrderPrice = order.OrderPrice,
                RegistrationDate = order.RegistrationDate,
                OrderItems = order.OrderItems.Select(oi => new OrderItemViewModel
                {
                    ProductName = oi.Product.Name,
                    Quantity = oi.Quantity,
                    TotalPrice = oi.TotalPrice
                }).ToList()
            };

            _logger.LogInformation("Відкрито деталі замовлення з Id {OrderId}", id);
            return View(model);
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

            var model = new OrderViewModel
            {
                Id = order.Id,
                Name = order.Name,
                LastName = order.LastName,
                Email = order.Email,
                Phone = order.Phone,
                DeliveryServiceName = order.DeliveryService.Name,
                DeliveryDepartmentName = order.DeliveryDepartment.Name,
                StatusTypeName = order.StatusType.Name,
                OrderPrice = order.OrderPrice,
                RegistrationDate = order.RegistrationDate
            };

            _logger.LogInformation("Відкрито сторінку підтвердження видалення замовлення з Id {OrderId}", id);
            return View(model);
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
                DeliveryServices = new SelectList(_context.DeliveryServices, "Id", "Name"),
                DeliveryDepartments = new SelectList(Enumerable.Empty<SelectListItem>())
            };

            return View(model);
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
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

            ModelState.Remove("CartItems");
            ModelState.Remove("DeliveryServices");
            ModelState.Remove("DeliveryDepartments");
            ModelState.Remove("TotalPrice");

            if (!ModelState.IsValid)
            {
                model.CartItems = cartItems;
                model.TotalPrice = cartItems.Sum(oi => oi.TotalPrice);
                model.DeliveryServices = new SelectList(_context.DeliveryServices, "Id", "Name", model.DeliveryServiceId);
                model.DeliveryDepartments = new SelectList(
                    _context.DeliveryDepartments
                        .Where(dd => dd.DeliveryServiceId == model.DeliveryServiceId),
                    "Id", "Name", model.DeliveryDepartmentId);
                _logger.LogWarning("Помилка валідації моделі: {Errors}", string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return View(model);
            }

            try
            {
                var order = new Order
                {
                    CustomerId = null,
                    Name = model.Name,
                    LastName = model.LastName,
                    Email = model.Email,
                    Phone = model.Phone,
                    StatusTypeId = 1,
                    DeliveryServiceId = model.DeliveryServiceId,
                    DeliveryDepartmentId = model.DeliveryDepartmentId,
                    RegistrationDate = DateTime.UtcNow,
                    OrderPrice = cartItems.Sum(oi => oi.TotalPrice)
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Замовлення створено з Id: {OrderId}", order.Id);

                foreach (var item in cartItems)
                {
                    item.OrderId = order.Id;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Елементи кошика прив’язано до замовлення {OrderId}", order.Id);

                TempData["SuccessMessage"] = "Замовлення успішно створено!";
                return RedirectToAction("Index", "Orders");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при створенні замовлення");
                ModelState.AddModelError("", $"Помилка при створенні замовлення: {ex.Message}");
                model.CartItems = cartItems;
                model.TotalPrice = cartItems.Sum(oi => oi.TotalPrice);
                model.DeliveryServices = new SelectList(_context.DeliveryServices, "Id", "Name", model.DeliveryServiceId);
                model.DeliveryDepartments = new SelectList(
                    _context.DeliveryDepartments
                        .Where(dd => dd.DeliveryServiceId == model.DeliveryServiceId),
                    "Id", "Name", model.DeliveryDepartmentId);
                return View(model);
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

    public class OrderViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string DeliveryServiceName { get; set; }
        public string DeliveryDepartmentName { get; set; }
        public string StatusTypeName { get; set; }
        public decimal OrderPrice { get; set; }
        public DateTime RegistrationDate { get; set; }
    }

    public class OrderDetailsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string DeliveryServiceName { get; set; }
        public string DeliveryDepartmentName { get; set; }
        public string StatusTypeName { get; set; }
        public decimal OrderPrice { get; set; }
        public DateTime RegistrationDate { get; set; }
        public List<OrderItemViewModel> OrderItems { get; set; }
    }

    public class OrderItemViewModel
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
    }
}