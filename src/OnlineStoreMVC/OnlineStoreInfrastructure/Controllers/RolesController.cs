using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineStoreDomain.Models;
using OnlineStoreInfrastructure.ViewModel;
using System.Linq;
using System.Threading.Tasks;
using OnlineStoreInfrastructure;
using OnlineStoreDomain.Model;
using Microsoft.AspNetCore.Authorization;

namespace OnlineStoreInfrastructure.Controllers
{
    public class RolesController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<User> _userManager;
        private readonly OnlineStoreContext _context;
        private readonly IdentityContext _identityContext;

        public RolesController(RoleManager<IdentityRole> roleManager, UserManager<User> userManager, OnlineStoreContext context, IdentityContext identityContext)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _context = context;
            _identityContext = identityContext;
        }

        public IActionResult Index()
        {
            return View(_roleManager.Roles.ToList());
        }

        public IActionResult UserList()
        {
            return View(_userManager.Users.ToList());
        }

        public async Task<IActionResult> Edit(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                var allRoles = _roleManager.Roles.Select(r => r.Name).ToList();
                var model = new ChangeRoleViewModel
                {
                    UserId = user.Id,
                    UserEmail = user.Email,
                    UserRoles = userRoles,
                    AllRoles = allRoles
                };
                return View(model);
            }

            return NotFound();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string userId, List<string> roles)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                var addedRoles = roles.Except(userRoles);
                var removedRoles = userRoles.Except(roles);

                await _userManager.AddToRolesAsync(user, addedRoles);
                await _userManager.RemoveFromRolesAsync(user, removedRoles);

                return RedirectToAction("UserList");
            }

            return NotFound();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Оновлення статусу замовлень на "Скасоване" (StatusTypeId = 4) замість видалення
            var orders = _context.Orders.Where(o => o.UserId == userId).ToList();
            foreach (var order in orders)
            {
                order.StatusTypeId = 4; // Припускаємо, що 4 - це ID статусу "Скасоване"
                _context.Update(order);
            }

            // Видалення пов’язаних даних із OnlineStoreContext
            var orderItems = _context.OrderItems.Where(oi => oi.UserId == userId).ToList();
            _context.OrderItems.RemoveRange(orderItems);

            var reviews = _context.Reviews.Where(r => r.UserId == userId).ToList();
            _context.Reviews.RemoveRange(reviews);

            var ratings = _context.ProductRatings.Where(r => r.UserId == userId).ToList();
            _context.ProductRatings.RemoveRange(ratings);

            await _context.SaveChangesAsync();

            // Видалення користувача з IdentityContext
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Користувача та пов’язані дані (крім замовлень) успішно видалено! Замовлення скасовано.";
                return RedirectToAction("UserList");
            }

            TempData["ErrorMessage"] = "Помилка при видаленні користувача: " + string.Join("; ", result.Errors.Select(e => e.Description));
            return RedirectToAction("UserList");
        }
    }
}