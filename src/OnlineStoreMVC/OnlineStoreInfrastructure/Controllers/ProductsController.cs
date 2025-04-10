using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineStoreDomain.Model;
using OnlineStoreInfrastructure;

namespace OnlineStoreInfrastructure.Controllers
{
    public class ProductsController : Controller
    {
        private readonly OnlineStoreContext _context;

        public ProductsController(OnlineStoreContext context)
        {
            _context = context;
        }

        // GET: Products
        public async Task<IActionResult> Index(int? id, string? name, bool all = false)
        {
            var products = _context.Products.Include(p => p.Category).AsQueryable();
            if (id.HasValue && !all)
            {
                products = products.Where(p => p.CategoryId == id);
                ViewBag.CategoryId = id;
                ViewBag.CategoryName = name ?? _context.Categories.FirstOrDefault(c => c.Id == id)?.Name ?? "Невідома категорія";
            }
            else
            {
                ViewBag.CategoryId = null;
                ViewBag.CategoryName = "Усі товари";
            }
            return View(await products.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id, bool returnAll = false, int? returnId = null, string returnName = null)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category) // Завантажуємо категорію
                .Include(p => p.Reviews)  // Завантажуємо відгуки
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            ViewBag.ReturnAll = returnAll;
            ViewBag.ReturnId = returnId;
            ViewBag.ReturnName = returnName;

            return View(product);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CategoryId,Name,GeneralInfo,Characteristics,Ratings,Quantity")] Product product)
        {
            Console.WriteLine("Сирі дані з форми:");
            foreach (var key in Request.Form.Keys)
            {
                Console.WriteLine($"{key}: {Request.Form[key]}");
            }
            Console.WriteLine($"Product: CategoryId={product.CategoryId}, Name={product.Name}, GeneralInfo={product.GeneralInfo}, Characteristics={product.Characteristics}, Ratings={product.Ratings}, Quantity={product.Quantity}");

            // Очищаємо ModelState і додаємо ручну валідацію
            ModelState.Clear();

            // Валідація CategoryId
            if (product.CategoryId <= 0 || !_context.Categories.Any(c => c.Id == product.CategoryId))
            {
                ModelState.AddModelError("CategoryId", "Обрана категорія не існує або не вибрана.");
            }

            // Валідація Name
            if (string.IsNullOrEmpty(product.Name))
            {
                ModelState.AddModelError("Name", "Назва продукту обов’язкова.");
            }
            else if (_context.Products.Any(p => p.Name == product.Name && p.CategoryId == product.CategoryId))
            {
                ModelState.AddModelError("Name", "Товар із такою назвою уже існує в цій категорії.");
            }

            // Валідація Ratings
            if (product.Ratings.HasValue && (product.Ratings < 1 || product.Ratings > 10))
            {
                ModelState.AddModelError("Ratings", "Рейтинг має бути від 1 до 10.");
            }

            // Валідація Quantity
            if (product.Quantity < 0)
            {
                ModelState.AddModelError("Quantity", "Кількість не може бути від’ємною.");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.SelectMany(x => x.Value.Errors).Select(x => x.ErrorMessage);
                TempData["ValidationErrors"] = string.Join("; ", errors);
                Console.WriteLine("Помилки валідації: " + TempData["ValidationErrors"]);
                ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
                return View(product);
            }

            try
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                Console.WriteLine("Товар успішно створено");
                return RedirectToAction(nameof(Index), new { all = true });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Помилка при створенні товару: {ex.Message}");
                Console.WriteLine($"Виняток: {ex.Message}");
                ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
                return View(product);
            }
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id, bool returnAll = false, int? returnId = null, string returnName = null)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            Console.WriteLine($"Завантажено продукт: Id={product.Id}, Quantity={product.Quantity}"); // Додано для дебагу

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewBag.ReturnAll = returnAll;
            ViewBag.ReturnId = returnId;
            ViewBag.ReturnName = returnName;

            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CategoryId,Name,GeneralInfo,Characteristics,Ratings,Quantity,Id")] Product product, bool returnAll = false, int? returnId = null, string returnName = null)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            Console.WriteLine("Сирі дані з форми:");
            foreach (var key in Request.Form.Keys)
            {
                Console.WriteLine($"{key}: {Request.Form[key]}");
            }
            Console.WriteLine($"Product: Id={product.Id}, CategoryId={product.CategoryId}, Name={product.Name}, GeneralInfo={product.GeneralInfo}, Characteristics={product.Characteristics}, Ratings={product.Ratings}, Quantity={product.Quantity}");
            Console.WriteLine($"returnAll: {returnAll}, returnId: {returnId}, returnName: {returnName}");

            // Очищаємо ModelState і додаємо ручну валідацію
            ModelState.Clear();

            if (product.CategoryId <= 0 || !_context.Categories.Any(c => c.Id == product.CategoryId))
            {
                ModelState.AddModelError("CategoryId", "Обрана категорія не існує.");
            }

            if (string.IsNullOrEmpty(product.Name))
            {
                ModelState.AddModelError("Name", "Назва продукту обов’язкова.");
            }
            else if (_context.Products.Any(p => p.Name == product.Name && p.CategoryId == product.CategoryId && p.Id != id))
            {
                ModelState.AddModelError("Name", "Товар із такою назвою уже існує в цій категорії.");
            }

            if (product.Ratings.HasValue && (product.Ratings < 1 || product.Ratings > 10))
            {
                ModelState.AddModelError("Ratings", "Рейтинг має бути від 1 до 10.");
            }

            // Валідація Quantity
            if (product.Quantity < 0)
            {
                ModelState.AddModelError("Quantity", "Кількість не може бути від’ємною.");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.SelectMany(x => x.Value.Errors).Select(x => x.ErrorMessage);
                TempData["ValidationErrors"] = string.Join("; ", errors);
                Console.WriteLine("Помилки валідації: " + TempData["ValidationErrors"]);
                ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
                ViewBag.ReturnAll = returnAll;
                ViewBag.ReturnId = returnId;
                ViewBag.ReturnName = returnName;
                return View(product);
            }

            try
            {
                var existingProduct = await _context.Products.FindAsync(id);
                if (existingProduct == null)
                {
                    return NotFound();
                }

                existingProduct.CategoryId = product.CategoryId;
                existingProduct.Name = product.Name;
                existingProduct.GeneralInfo = product.GeneralInfo;
                existingProduct.Characteristics = product.Characteristics;
                existingProduct.Ratings = product.Ratings;
                existingProduct.Quantity = product.Quantity; // Оновлюємо Quantity

                _context.Update(existingProduct);
                await _context.SaveChangesAsync();
                Console.WriteLine("Збереження виконано");

                if (returnAll)
                {
                    return RedirectToAction(nameof(Index), new { all = true });
                }
                return RedirectToAction(nameof(Index), new { id = returnId, name = returnName });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Помилка при збереженні: {ex.Message}");
                Console.WriteLine($"Виняток: {ex.Message}");
                ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
                ViewBag.ReturnAll = returnAll;
                ViewBag.ReturnId = returnId;
                ViewBag.ReturnName = returnName;
                return View(product);
            }
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id, bool returnAll = false, int? returnId = null, string returnName = null)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            Console.WriteLine($"Завантажено продукт для видалення: Id={product.Id}, Quantity={product.Quantity}"); // Додано для дебагу

            ViewBag.ReturnAll = returnAll;
            ViewBag.ReturnId = returnId;
            ViewBag.ReturnName = returnName;

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, bool returnAll = false, int? returnId = null, string returnName = null)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }

            if (returnAll)
            {
                return RedirectToAction(nameof(Index), new { all = true });
            }
            return RedirectToAction(nameof(Index), new { id = returnId, name = returnName });
        }
    }
}