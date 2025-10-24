using ASP_421.Data;
using ASP_421.Data.Entities;
using ASP_421.Models.Shop.Api;
using ASP_421.Services.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ASP_421.Controllers.Api
{
    [Route("api/product")]
    [ApiController]
    public class ProductController(
            IStorageService storageService,
            ILogger<ProductController> logger,
            DataAccessor dataAccessor
        ) : ControllerBase
    {
        private readonly IStorageService _storageService = storageService;
        private readonly ILogger<ProductController> _logger = logger;
        private readonly DataAccessor _dataAccessor = dataAccessor;

        [HttpGet]
        public object GetAllProducts()
        {
            try
            {
                // Получаем все товары напрямую из контекста
                var products = _dataAccessor.ProductGroups()
                    .SelectMany(g => g.Products.Where(p => p.DeletedAt == null))
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Description,
                        p.Price,
                        p.Stock,
                        p.GroupId,
                        GroupName = p.Group.Name,
                    })
                    .ToList();

                return new
                {
                    Status = "Ok",
                    Data = products,
                    Count = products.Count
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    Status = "Fail",
                    ErrorMessage = ex.Message
                };
            }
        }

        [HttpPost]
        public object CreateProduct(ShopApiProductFormModel formModel)
        {
            // Валидация модели
            var validationErrors = new Dictionary<string, string>();

            // Валидация названия товара
            if (string.IsNullOrWhiteSpace(formModel.Name))
            {
                validationErrors["Name"] = "Назва товару не може бути порожньою";
            }
            else if (formModel.Name.Length < 2)
            {
                validationErrors["Name"] = "Назва товару повинна містити мінімум 2 символи";
            }
            else if (formModel.Name.Length > 100)
            {
                validationErrors["Name"] = "Назва товару не може бути довшою за 100 символів";
            }

            // Валидация группы товара
            if (string.IsNullOrWhiteSpace(formModel.GroupId))
            {
                validationErrors["GroupId"] = "Група товару обов'язкова";
            }
            else if (!Guid.TryParse(formModel.GroupId, out _))
            {
                validationErrors["GroupId"] = "Невірний ID групи товару";
            }

            // Валидация цены
            if (formModel.Price < 0)
            {
                validationErrors["Price"] = "Ціна не може бути від'ємною";
            }
            else if (formModel.Price > 999999.99)
            {
                validationErrors["Price"] = "Ціна не може перевищувати 999,999.99";
            }

            // Валидация количества на складе
            if (formModel.Stock < 0)
            {
                validationErrors["Stock"] = "Кількість на складі не може бути від'ємною";
            }

            // Валидация slug
            if (!string.IsNullOrWhiteSpace(formModel.Slug))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(formModel.Slug, @"^[a-z0-9\-]+$"))
                {
                    validationErrors["Slug"] = "Slug може містити тільки малі літери, цифри та дефіси";
                }
            }

            // Валидация изображения
            if (formModel.Image != null)
            {
                if (formModel.Image.Length > 5 * 1024 * 1024) // 5MB
                {
                    validationErrors["Image"] = "Розмір зображення не може перевищувати 5MB";
                }
                else
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
                    var extension = Path.GetExtension(formModel.Image.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(extension))
                    {
                        validationErrors["Image"] = $"Дозволені формати: {string.Join(", ", allowedExtensions)}";
                    }
                }
            }

            // Если есть ошибки валидации, возвращаем их
            if (validationErrors.Count > 0)
            {
                return new
                {
                    Status = "ValidationError",
                    Errors = validationErrors
                };
            }

        // Сохраняем изображение, если оно есть
        string? imageUrl = null;
        
        try
        {
                _logger.LogInformation($"Image file received: {(formModel.Image != null ? $"File: {formModel.Image.FileName}, Size: {formModel.Image.Length}" : "null")}");
                
                if (formModel.Image != null && formModel.Image.Length > 0)
                {
                    try
                    {
                        _logger.LogInformation($"Saving image file: {formModel.Image.FileName}");
                        imageUrl = _storageService.Save(formModel.Image);
                        _logger.LogInformation($"Image saved successfully: {imageUrl}");
                    }
                    catch (Exception imgEx)
                    {
                        _logger.LogError($"Error saving image: {imgEx.Message}");
                        return new
                        {
                            Status = "Fail",
                            ErrorMessage = "Помилка збереження зображення: " + imgEx.Message
                        };
                    }
                }
                else
                {
                    _logger.LogInformation("No image file provided or file is empty");
                }

                // Генерируем уникальный slug
                string baseSlug = string.IsNullOrWhiteSpace(formModel.Slug) 
                    ? formModel.Name.Trim().ToLowerInvariant() 
                    : formModel.Slug.Trim().ToLowerInvariant();
                
                baseSlug = System.Text.RegularExpressions.Regex.Replace(baseSlug, @"[^a-z0-9]+", "-").Trim('-');
                
                string uniqueSlug = baseSlug;
                int counter = 1;
                
                // Проверяем уникальность slug
                while (!_dataAccessor.IsProductSlugUnique(uniqueSlug))
                {
                    uniqueSlug = $"{baseSlug}-{counter}";
                    counter++;
                }

                Product product = new()
                {
                    Name = formModel.Name.Trim(),
                    Description = string.IsNullOrWhiteSpace(formModel.Description) ? null : formModel.Description.Trim(),
                    Slug = uniqueSlug,
                    Stock = formModel.Stock,
                    Price = (decimal)formModel.Price,
                    GroupId = Guid.Parse(formModel.GroupId),
                    ImageUrl = imageUrl
                };

                _logger.LogInformation($"Creating product: {product.Name}, Slug: {product.Slug}, GroupId: {product.GroupId}, ImageUrl: {product.ImageUrl}");
                _dataAccessor.AddProduct(product);
                _logger.LogInformation($"Product created successfully with ID: {product.Id}");
                
                return new
                {
                    Status = "Ok",
                    Message = "Товар успішно створений",
                    ProductId = product.Id
                };
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("slug"))
            {
                _logger.LogWarning($"Slug conflict detected: {ex.Message}. Attempting to generate new slug.");
                
                // Попробуем создать товар с новым slug
                try
                {
                    // Генерируем новый уникальный slug
                    string newBaseSlug = System.Text.RegularExpressions.Regex.Replace(
                        formModel.Name.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
                    
                    string newUniqueSlug = newBaseSlug;
                    int counter = 1;
                    
                    while (!_dataAccessor.IsProductSlugUnique(newUniqueSlug))
                    {
                        newUniqueSlug = $"{newBaseSlug}-{counter}";
                        counter++;
                    }
                    
                    // Создаем новый продукт с новым slug
                    Product retryProduct = new()
                    {
                        Id = Guid.NewGuid(),
                        Name = formModel.Name.Trim(),
                        Description = string.IsNullOrWhiteSpace(formModel.Description) ? null : formModel.Description.Trim(),
                        Slug = newUniqueSlug,
                        Stock = formModel.Stock,
                        Price = (decimal)formModel.Price,
                        GroupId = Guid.Parse(formModel.GroupId),
                        ImageUrl = imageUrl,
                        DeletedAt = null
                    };
                    
                    _logger.LogInformation($"Retrying with new slug: {newUniqueSlug}");
                    _dataAccessor.AddProduct(retryProduct);
                    
                    return new
                    {
                        Status = "Ok",
                        Message = "Товар успішно створений з автоматично згенерованим slug",
                        ProductId = retryProduct.Id
                    };
                }
                catch (Exception retryEx)
                {
                    _logger.LogError(retryEx, "Error creating product with retry slug");
                    return new
                    {
                        Status = "Fail",
                        ErrorMessage = "Помилка створення товару після повторної спроби: " + retryEx.Message
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return new
                {
                    Status = "Fail",
                    ErrorMessage = "Помилка створення товару: " + ex.Message
                };
            }
        }

    }
}
