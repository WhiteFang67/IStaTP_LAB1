using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineStoreInfrastructure;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineStoreInfrastructure.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChartsController : ControllerBase
    {
        private readonly OnlineStoreContext _context;

        public ChartsController(OnlineStoreContext context)
        {
            _context = context;
        }

        private record ProductsByCategoryResponseItem(string CategoryName, int ProductCount);
        private record ProductQuantityResponseItem(string ProductName, int Quantity); // Новий рекорд для другої діаграми

        [HttpGet("productsByCategory")]
        public async Task<JsonResult> GetProductsByCategoryAsync()
        {
            var responseItems = await _context.Products
                .GroupBy(p => p.CategoryId)
                .Select(g => new
                {
                    CategoryId = g.Key,
                    ProductCount = g.Count()
                })
                .Join(_context.Categories,
                      productGroup => productGroup.CategoryId,
                      category => category.Id,
                      (productGroup, category) => new ProductsByCategoryResponseItem(
                          category.Name,
                          productGroup.ProductCount))
                .ToListAsync();

            return new JsonResult(responseItems);
        }

        [HttpGet("productQuantities")]
        public async Task<JsonResult> GetProductQuantitiesAsync()
        {
            var responseItems = await _context.Products
                .Select(p => new ProductQuantityResponseItem(
                    p.Name,
                    p.Quantity))
                .ToListAsync();

            return new JsonResult(responseItems);
        }
    }
}