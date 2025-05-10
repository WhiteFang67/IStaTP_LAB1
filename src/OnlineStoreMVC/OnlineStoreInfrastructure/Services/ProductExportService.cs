using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineStoreDomain.Model;
using OnlineStoreInfrastructure;

namespace OnlineStoreInfrastructure.Services
{
    public class ProductExportService : IExportService<Product>
    {
        private readonly OnlineStoreContext _context;
        private readonly ILogger<ProductExportService> _logger;

        public ProductExportService(OnlineStoreContext context, ILogger<ProductExportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task WriteToAsync(Stream stream, CancellationToken cancellationToken)
        {
            if (!stream.CanWrite)
            {
                _logger.LogError("Input stream is not writable");
                throw new ArgumentException("Input stream is not writable");
            }

            try
            {
                // Load all products with their categories
                var products = await _context.Products
                    .Include(p => p.Category)
                    .ToListAsync(cancellationToken);

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Products");

                // Write headers (Ukrainian localized names, excluding Ratings)
                var headers = new[] { "Категорія", "Назва", "Характеристики", "Кількість", "Ціна" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                }

                // Write products
                int row = 2;
                foreach (var product in products)
                {
                    worksheet.Cell(row, 1).Value = product.Category?.Name ?? "Невідома категорія";
                    worksheet.Cell(row, 2).Value = product.Name;
                    worksheet.Cell(row, 3).Value = product.Characteristics ?? "";
                    worksheet.Cell(row, 4).Value = product.Quantity;
                    worksheet.Cell(row, 5).Value = product.Price;

                    row++;
                }

                // Auto-fit columns for better readability
                worksheet.Columns().AdjustToContents();

                // Save to stream
                workbook.SaveAs(stream);
                _logger.LogInformation("Exported {Count} products to Excel file", products.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting products to Excel");
                throw;
            }
        }
    }
}