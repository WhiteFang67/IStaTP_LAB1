using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineStoreDomain.Model;
using OnlineStoreInfrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OnlineStoreInfrastructure.Services
{
    public class ProductImportService : IImportService<Product>
    {
        private readonly OnlineStoreContext _context;
        private readonly ILogger<ProductImportService> _logger;

        public ProductImportService(OnlineStoreContext context, ILogger<ProductImportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ImportFromStreamAsync(Stream stream, CancellationToken cancellationToken)
        {
            if (!stream.CanRead)
            {
                _logger.LogError("Input stream is not readable");
                throw new ArgumentException("Input stream is not readable");
            }

            var errors = new List<string>();

            try
            {
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet("Products") ?? workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    _logger.LogError("No worksheets found in the Excel file");
                    errors.Add("Файл Excel не містить аркушів");
                    throw new InvalidOperationException("No worksheets found");
                }

                // Validate headers
                var headers = worksheet.Row(1).Cells(1, 7).Select(c => c.GetString().Trim()).ToList();
                var expectedHeaders = new[] { "Категорія", "Назва", "Огляд", "Характеристики", "Рейтинг", "Кількість", "Ціна" };
                if (!headers.SequenceEqual(expectedHeaders))
                {
                    _logger.LogError("Invalid header format in Excel file. Expected: {Expected}, Found: {Found}",
                        string.Join(", ", expectedHeaders), string.Join(", ", headers));
                    errors.Add("Невірний формат заголовків у файлі Excel");
                    throw new InvalidOperationException("Invalid header format");
                }

                // Process rows
                int row = 2;
                while (!worksheet.Row(row).IsEmpty())
                {
                    try
                    {
                        // Read row data
                        var categoryName = worksheet.Cell(row, 1).GetString()?.Trim();
                        var name = worksheet.Cell(row, 2).GetString()?.Trim();
                        var generalInfo = worksheet.Cell(row, 3).GetString()?.Trim();
                        var characteristics = worksheet.Cell(row, 4).GetString()?.Trim();
                        var ratingsStr = worksheet.Cell(row, 5).GetString()?.Trim();
                        var quantityStr = worksheet.Cell(row, 6).GetString()?.Trim();
                        var priceStr = worksheet.Cell(row, 7).GetString()?.Trim();

                        // Validation: Required fields
                        if (string.IsNullOrWhiteSpace(categoryName))
                        {
                            errors.Add($"Рядок {row}: Назва категорії не вказана");
                            row++;
                            continue;
                        }
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            errors.Add($"Рядок {row}: Назва товару не вказана");
                            row++;
                            continue;
                        }
                        if (string.IsNullOrWhiteSpace(quantityStr))
                        {
                            errors.Add($"Рядок {row}: Кількість не вказана");
                            row++;
                            continue;
                        }
                        if (string.IsNullOrWhiteSpace(priceStr))
                        {
                            errors.Add($"Рядок {row}: Ціна не вказана");
                            row++;
                            continue;
                        }

                        // Validation: Category existence
                        var category = await _context.Categories
                            .FirstOrDefaultAsync(c => c.Name == categoryName, cancellationToken);
                        if (category == null)
                        {
                            errors.Add($"Рядок {row}: Категорія '{categoryName}' не знайдена");
                            row++;
                            continue;
                        }

                        // Validation: Quantity
                        if (!int.TryParse(quantityStr, out int quantity) || quantity < 0)
                        {
                            errors.Add($"Рядок {row}: Кількість має бути невід'ємним цілим числом");
                            row++;
                            continue;
                        }

                        // Validation: Price
                        if (!decimal.TryParse(priceStr, out decimal price) || price <= 0)
                        {
                            errors.Add($"Рядок {row}: Ціна має бути додатним числом");
                            row++;
                            continue;
                        }

                        // Validation: Ratings
                        float? ratings = null;
                        if (!string.IsNullOrWhiteSpace(ratingsStr))
                        {
                            if (!float.TryParse(ratingsStr, out float parsedRatings) || parsedRatings < 0 || parsedRatings > 5)
                            {
                                errors.Add($"Рядок {row}: Рейтинг має бути числом від 0 до 5");
                                row++;
                                continue;
                            }
                            ratings = parsedRatings;
                        }

                        // Normalize empty strings to null
                        generalInfo = string.IsNullOrWhiteSpace(generalInfo) ? null : generalInfo;
                        characteristics = string.IsNullOrWhiteSpace(characteristics) ? null : characteristics;

                        // Check for existing product
                        var existingProduct = await _context.Products
                            .FirstOrDefaultAsync(p => p.Name == name && p.CategoryId == category.Id, cancellationToken);

                        if (existingProduct != null)
                        {
                            // Check for changes
                            bool hasChanges = false;
                            if (existingProduct.GeneralInfo != generalInfo)
                            {
                                existingProduct.GeneralInfo = generalInfo;
                                hasChanges = true;
                            }
                            if (existingProduct.Characteristics != characteristics)
                            {
                                existingProduct.Characteristics = characteristics;
                                hasChanges = true;
                            }
                            if (existingProduct.Ratings != ratings)
                            {
                                existingProduct.Ratings = ratings;
                                hasChanges = true;
                            }
                            if (existingProduct.Quantity != quantity)
                            {
                                existingProduct.Quantity = quantity;
                                hasChanges = true;
                            }
                            if (existingProduct.Price != price)
                            {
                                existingProduct.Price = price;
                                hasChanges = true;
                            }

                            if (hasChanges)
                            {
                                _context.Update(existingProduct);
                                _logger.LogInformation("Updated product '{Name}' in category '{Category}' at row {Row} with changes", name, categoryName, row);
                            }
                            else
                            {
                                _logger.LogInformation("No changes for product '{Name}' in category '{Category}' at row {Row}", name, categoryName, row);
                            }
                        }
                        else
                        {
                            // Create new product
                            var newProduct = new Product
                            {
                                CategoryId = category.Id,
                                Name = name,
                                GeneralInfo = generalInfo,
                                Characteristics = characteristics,
                                Ratings = ratings,
                                Quantity = quantity,
                                Price = price
                            };
                            _context.Add(newProduct);
                            _logger.LogInformation("Added new product '{Name}' in category '{Category}' at row {Row}", name, categoryName, row);
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Рядок {row}: Помилка обробки: {ex.Message}");
                        _logger.LogError(ex, "Error processing row {Row}", row);
                    }
                    row++;
                }

                // Check for errors before saving
                if (errors.Any())
                {
                    _logger.LogWarning("Import aborted due to {ErrorCount} errors: {Errors}", errors.Count, string.Join("; ", errors));
                    throw new InvalidOperationException(string.Join("; ", errors));
                }

                // Save changes
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully imported products from Excel file");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing products from Excel");
                throw;
            }
        }
    }
}