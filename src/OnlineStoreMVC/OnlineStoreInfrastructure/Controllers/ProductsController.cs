using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineStoreDomain.Model;
using OnlineStoreInfrastructure;
using OnlineStoreInfrastructure.Services;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

namespace OnlineStoreInfrastructure.Controllers
{
    public class ProductsController : Controller
    {
        private readonly OnlineStoreContext _context;
        private readonly IExportService<Product> _exportService;
        private readonly IImportService<Product> _importService;

        public ProductsController(
            OnlineStoreContext context,
            IExportService<Product> exportService,
            IImportService<Product> importService)
        {
            _context = context;
            _exportService = exportService;
            _importService = importService;
        }

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
                .Include(p => p.Category)
                .Include(p => p.Reviews)
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

        [Authorize(Roles = "admin")]
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([Bind("CategoryId,Name,GeneralInfo,Characteristics,Quantity,Price")] Product product)
        {
            Console.WriteLine("Сирі дані з форми:");
            foreach (var key in Request.Form.Keys)
            {
                Console.WriteLine($"{key}: {Request.Form[key]}");
            }
            Console.WriteLine($"Product: CategoryId={product.CategoryId}, Name={product.Name}, GeneralInfo={product.GeneralInfo}, Characteristics={product.Characteristics}, Quantity={product.Quantity}, Price={product.Price}");

            ModelState.Clear();

            if (product.CategoryId <= 0 || !_context.Categories.Any(c => c.Id == product.CategoryId))
            {
                ModelState.AddModelError("CategoryId", "Обрана категорія не існує або не вибрана.");
            }

            if (string.IsNullOrEmpty(product.Name))
            {
                ModelState.AddModelError("Name", "Назва продукту обов’язкова.");
            }
            else if (_context.Products.Any(p => p.Name == product.Name && p.CategoryId == product.CategoryId))
            {
                ModelState.AddModelError("Name", "Товар із такою назвою уже існує в цій категорії.");
            }

            if (product.Quantity < 0)
            {
                ModelState.AddModelError("Quantity", "Кількість не може бути від’ємною.");
            }

            if (product.Price <= 0)
            {
                ModelState.AddModelError("Price", "Ціна повинна бути більшою за 0.");
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
                product.Ratings = null;
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

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Edit(int? id, string returnTo = "Index", bool returnAll = false, int? returnId = null, string returnName = null)
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

            Console.WriteLine($"Завантажено продукт: Id={product.Id}, Quantity={product.Quantity}, Price={product.Price}");

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewBag.ReturnTo = returnTo;
            ViewBag.ReturnAll = returnAll;
            ViewBag.ReturnId = returnId;
            ViewBag.ReturnName = returnName;
            ViewBag.ProductId = id;

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Edit(int id, [Bind("CategoryId,Name,GeneralInfo,Characteristics,Quantity,Price,Id")] Product product, string returnTo = "Index", bool returnAll = false, int? returnId = null, string returnName = null)
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
            Console.WriteLine($"Product: Id={product.Id}, CategoryId={product.CategoryId}, Name={product.Name}, GeneralInfo={product.GeneralInfo}, Characteristics={product.Characteristics}, Quantity={product.Quantity}, Price={product.Price}");

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

            if (product.Quantity < 0)
            {
                ModelState.AddModelError("Quantity", "Кількість не може бути від’ємною.");
            }

            if (product.Price <= 0)
            {
                ModelState.AddModelError("Price", "Ціна повинна бути більшою за 0.");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.SelectMany(x => x.Value.Errors).Select(x => x.ErrorMessage);
                TempData["ValidationErrors"] = string.Join("; ", errors);
                Console.WriteLine("Помилки валідації: " + TempData["ValidationErrors"]);
                ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
                ViewBag.ReturnTo = returnTo;
                ViewBag.ReturnAll = returnAll;
                ViewBag.ReturnId = returnId;
                ViewBag.ReturnName = returnName;
                ViewBag.ProductId = id;
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
                existingProduct.Quantity = product.Quantity;
                existingProduct.Price = product.Price;

                _context.Update(existingProduct);
                await _context.SaveChangesAsync();
                Console.WriteLine("Збереження виконано");

                if (returnTo == "Details")
                {
                    if (returnAll)
                    {
                        return RedirectToAction("Details", new { id = product.Id, returnAll = true });
                    }
                    return RedirectToAction("Details", new { id = product.Id, returnId = returnId, returnName = returnName });
                }
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
                ViewBag.ReturnTo = returnTo;
                ViewBag.ReturnAll = returnAll;
                ViewBag.ReturnId = returnId;
                ViewBag.ReturnName = returnName;
                ViewBag.ProductId = id;
                return View(product);
            }
        }

        [Authorize(Roles = "admin")]
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

            Console.WriteLine($"Завантажено продукт для видалення: Id={product.Id}, Quantity={product.Quantity}, Price={product.Price}");

            ViewBag.ReturnAll = returnAll;
            ViewBag.ReturnId = returnId;
            ViewBag.ReturnName = returnName;

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
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

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Export(int? categoryId, CancellationToken cancellationToken)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                await _exportService.WriteToAsync(memoryStream, cancellationToken);
                var fileBytes = memoryStream.ToArray();

                var fileName = $"products_{DateTime.UtcNow:yyyy-MM-dd}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["ValidationErrors"] = $"Помилка при експорті: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Import(IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ValidationErrors"] = "Будь ласка, виберіть файл для імпорту";
                return RedirectToAction(nameof(Index), new { all = true });
            }

            if (file.ContentType != "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                TempData["ValidationErrors"] = "Підтримується лише формат .xlsx";
                return RedirectToAction(nameof(Index), new { all = true });
            }

            try
            {
                using var stream = file.OpenReadStream();
                await _importService.ImportFromStreamAsync(stream, cancellationToken);
                TempData["SuccessMessage"] = "Товари успішно імпортовано";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ValidationErrors"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["ValidationErrors"] = $"Помилка при імпорті: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { all = true });
        }
    }
}