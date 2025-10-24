using ASP_421.Data;
using ASP_421.Data.Entities;
using ASP_421.Models.Shop;
using ASP_421.Services.Storage;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace ASP_421.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminEditController(DataAccessor dataAccessor, IStorageService storageService) : ControllerBase
    {
        private readonly DataAccessor _dataAccessor = dataAccessor;
        private readonly IStorageService _storageService = storageService;

        // Получение всех групп для редактирования
        [HttpGet("groups")]
        public IActionResult GetGroups()
        {
            try
            {
                var groups = _dataAccessor.GetAllGroups()
                    .Select(g => new EditGroupViewModel
                    {
                        Id = g.Id,
                        Name = g.Name,
                        Description = g.Description,
                        ImageUrl = g.ImageUrl,
                        ParentId = g.ParentId,
                        Slug = g.Slug,
                        IsDeleted = g.DeletedAt != null,
                        ParentName = g.Parent?.Name,
                        ProductsCount = g.Products.Count(p => p.DeletedAt == null),
                        SubGroupsCount = _dataAccessor.GetSubGroupsCount(g.Id)
                    })
                    .ToList();

                return Ok(new { success = true, data = groups });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Помилка отримання груп: {ex.Message}" });
            }
        }

        // Получение всех товаров для редактирования
        [HttpGet("products")]
        public IActionResult GetProducts()
        {
            try
            {
                var products = _dataAccessor.GetAllProducts()
                    .Select(p => new EditProductViewModel
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        Stock = p.Stock,
                        ImageUrl = p.ImageUrl,
                        GroupId = p.GroupId ?? Guid.Empty,
                        Slug = p.Slug,
                        IsDeleted = p.DeletedAt != null,
                        GroupName = p.Group?.Name
                    })
                    .ToList();

                return Ok(new { success = true, data = products });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Помилка отримання товарів: {ex.Message}" });
            }
        }

        // Получение группы по ID
        [HttpGet("groups/{id}")]
        public IActionResult GetGroup(Guid id)
        {
            try
            {
                var group = _dataAccessor.GetGroupById(id);
                if (group == null)
                {
                    return NotFound(new { success = false, message = "Групу не знайдено" });
                }

                var groupViewModel = new EditGroupViewModel
                {
                    Id = group.Id,
                    Name = group.Name,
                    Description = group.Description,
                    ImageUrl = group.ImageUrl,
                    ParentId = group.ParentId,
                    Slug = group.Slug,
                    IsDeleted = group.DeletedAt != null,
                    ParentName = group.Parent?.Name,
                    ProductsCount = group.Products.Count(p => p.DeletedAt == null),
                    SubGroupsCount = _dataAccessor.GetSubGroupsCount(group.Id)
                };

                return Ok(new { success = true, data = groupViewModel });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Помилка отримання групи: {ex.Message}" });
            }
        }

        // Получение товара по ID
        [HttpGet("products/{id}")]
        public IActionResult GetProduct(Guid id)
        {
            // Проверка авторизации
            if (!HttpContext.User.Identity?.IsAuthenticated ?? true)
            {
                return Unauthorized(new { success = false, message = "Необхідна авторизація" });
            }

            var role = HttpContext.User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Forbid();
            }

            try
            {
                Console.WriteLine($"GET request for product with ID: {id}");
                
                var product = _dataAccessor.GetProductById(id);
                if (product == null)
                {
                    Console.WriteLine($"Product with ID {id} not found");
                    return NotFound(new { success = false, message = "Товар не знайдено" });
                }

                Console.WriteLine($"Found product: {product.Name}");

                var productViewModel = new EditProductViewModel
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Stock = product.Stock,
                    ImageUrl = product.ImageUrl,
                    GroupId = product.GroupId ?? Guid.Empty,
                    Slug = product.Slug,
                    IsDeleted = product.DeletedAt != null,
                    GroupName = product.Group?.Name
                };

                Console.WriteLine($"Returning product view model with ID: {productViewModel.Id}");
                Console.WriteLine($"ProductViewModel serialized: {System.Text.Json.JsonSerializer.Serialize(productViewModel)}");
                return Ok(new { success = true, data = productViewModel });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Помилка отримання товару: {ex.Message}" });
            }
        }

        // Обновление группы
        [HttpPut("groups/{id}")]
        public IActionResult UpdateGroup(Guid id, [FromBody] EditGroupViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                    return BadRequest(new { success = false, message = "Помилки валідації", errors });
                }

                var group = _dataAccessor.GetGroupById(id);
                if (group == null)
                {
                    return NotFound(new { success = false, message = "Групу не знайдено" });
                }

                // Проверка уникальности slug
                if (!_dataAccessor.IsGroupSlugUnique(model.Slug, id))
                {
                    return BadRequest(new { success = false, message = "Slug групи вже існує" });
                }

                // Обновление данных
                group.Name = model.Name;
                group.Description = model.Description;
                group.ImageUrl = model.ImageUrl;
                group.ParentId = model.ParentId;
                group.Slug = model.Slug;

                _dataAccessor.UpdateGroup(group);

                return Ok(new { success = true, message = "Групу успішно оновлено" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Помилка оновлення групи: {ex.Message}" });
            }
        }

        // Обновление товара
        [HttpPost("products/{id}/update")]
        public IActionResult UpdateProduct(Guid id, [FromForm] EditProductViewModel model, IFormFile? productImage)
        {
            Console.WriteLine($"PUT request received for product ID: {id}");
            Console.WriteLine($"Request content type: {HttpContext.Request.ContentType}");
            
            // Проверка авторизации
            Console.WriteLine($"User authenticated: {HttpContext.User.Identity?.IsAuthenticated}");
            Console.WriteLine($"User name: {HttpContext.User.Identity?.Name}");
            
            if (!HttpContext.User.Identity?.IsAuthenticated ?? true)
            {
                Console.WriteLine("User not authenticated, returning 401");
                return Unauthorized(new { success = false, message = "Необхідна авторизація" });
            }

            var role = HttpContext.User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            Console.WriteLine($"User role: {role}");

            if (role != "Admin")
            {
                Console.WriteLine("User is not Admin, returning 403");
                return Forbid();
            }

            try
            {
                Console.WriteLine($"Attempting to update product with ID: {id}");
                Console.WriteLine($"Model data - Name: {model.Name}, Price: {model.Price}, Stock: {model.Stock}, GroupId: {model.GroupId}, ImageUrl: {model.ImageUrl}");
                Console.WriteLine($"ProductImage: {(productImage != null ? $"File: {productImage.FileName}, Size: {productImage.Length}" : "null")}");
                
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                    Console.WriteLine($"Validation errors: {string.Join(", ", errors)}");
                    
                    // Логируем все ошибки ModelState
                    foreach (var key in ModelState.Keys)
                    {
                        var state = ModelState[key];
                        if (state.Errors.Count > 0)
                        {
                            Console.WriteLine($"ModelState error for '{key}': {string.Join(", ", state.Errors.Select(e => e.ErrorMessage))}");
                        }
                    }
                    
                    return BadRequest(new { success = false, message = "Помилки валідації", errors });
                }

                var product = _dataAccessor.GetProductById(id);
                if (product == null)
                {
                    Console.WriteLine($"Product with ID {id} not found");
                    return NotFound(new { success = false, message = "Товар не знайдено" });
                }

                Console.WriteLine($"Found product: {product.Name}");

                // Проверка уникальности названия в группе
                if (!_dataAccessor.IsProductNameUniqueInGroup(model.Name, model.GroupId, id))
                {
                    return BadRequest(new { success = false, message = "Товар з такою назвою вже існує в цій групі" });
                }

                // Проверка уникальности slug
                if (!_dataAccessor.IsProductSlugUnique(model.Slug, id))
                {
                    return BadRequest(new { success = false, message = "Slug товару вже існує" });
                }

                // Обработка загрузки нового изображения
                if (productImage != null && productImage.Length > 0)
                {
                    var imageUrl = _storageService.Save(productImage);
                    product.ImageUrl = imageUrl;
                }

                // Обновление данных
                product.Name = model.Name;
                product.Description = model.Description;
                product.Price = model.Price;
                product.Stock = model.Stock;
                if (productImage == null || productImage.Length == 0)
                {
                    product.ImageUrl = model.ImageUrl; // Сохраняем старое изображение, если новое не загружено
                }
                product.GroupId = model.GroupId;
                product.Slug = model.Slug;

                _dataAccessor.UpdateProduct(product);

                Console.WriteLine($"Product {product.Name} successfully updated");
                return Ok(new { success = true, message = "Товар успішно оновлено" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Помилка оновлення товару: {ex.Message}" });
            }
        }

        // Удаление группы (мягкое удаление)
        [HttpDelete("groups/{id}")]
        public IActionResult DeleteGroup(Guid id)
        {
            try
            {
                var group = _dataAccessor.GetGroupById(id);
                if (group == null)
                {
                    return NotFound(new { success = false, message = "Групу не знайдено" });
                }

                _dataAccessor.SoftDeleteGroup(id);

                return Ok(new { success = true, message = "Групу успішно видалено" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Помилка видалення групи: {ex.Message}" });
            }
        }

        // Удаление товара (мягкое удаление)
        [HttpDelete("products/{id}")]
        public IActionResult DeleteProduct(Guid id)
        {
            // Проверка авторизации
            if (!HttpContext.User.Identity?.IsAuthenticated ?? true)
            {
                return Unauthorized(new { success = false, message = "Необхідна авторизація" });
            }

            var role = HttpContext.User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                return Forbid();
            }

            try
            {
                Console.WriteLine($"Attempting to delete product with ID: {id}");
                
                var product = _dataAccessor.GetProductById(id);
                if (product == null)
                {
                    Console.WriteLine($"Product with ID {id} not found");
                    return NotFound(new { success = false, message = "Товар не знайдено" });
                }

                Console.WriteLine($"Found product: {product.Name}, deleting...");
                _dataAccessor.SoftDeleteProduct(id);
                Console.WriteLine($"Product {product.Name} successfully deleted");

                return Ok(new { success = true, message = "Товар успішно видалено" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Помилка видалення товару: {ex.Message}" });
            }
        }

        // Проверка уникальности slug группы
        [HttpGet("groups/check-slug")]
        public IActionResult CheckGroupSlug([FromQuery] string slug, [FromQuery] Guid? excludeId = null)
        {
            try
            {
                var isUnique = _dataAccessor.IsGroupSlugUnique(slug, excludeId);
                return Ok(new { success = true, isUnique });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Помилка перевірки slug: {ex.Message}" });
            }
        }

        // Проверка уникальности slug товара
        [HttpGet("products/check-slug")]
        public IActionResult CheckProductSlug([FromQuery] string slug, [FromQuery] Guid? excludeId = null)
        {
            try
            {
                var isUnique = _dataAccessor.IsProductSlugUnique(slug, excludeId);
                return Ok(new { success = true, isUnique });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Помилка перевірки slug: {ex.Message}" });
            }
        }

        // Проверка уникальности названия товара в группе
        [HttpGet("products/check-name")]
        public IActionResult CheckProductName([FromQuery] string name, [FromQuery] Guid groupId, [FromQuery] Guid? excludeId = null)
        {
            try
            {
                var isUnique = _dataAccessor.IsProductNameUniqueInGroup(name, groupId, excludeId);
                return Ok(new { success = true, isUnique });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Помилка перевірки назви: {ex.Message}" });
            }
        }

        // Генерация slug из названия
        [HttpPost("generate-slug")]
        public IActionResult GenerateSlug([FromBody] GenerateSlugRequest request)
        {
            try
            {
                var slug = GenerateSlugFromText(request.Text);
                return Ok(new { success = true, slug });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Помилка генерації slug: {ex.Message}" });
            }
        }

        private string GenerateSlugFromText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Транслитерация украинских символов
            var transliteration = new Dictionary<char, string>
            {
                {'а', "a"}, {'б', "b"}, {'в', "v"}, {'г', "g"}, {'д', "d"}, {'е', "e"}, {'є', "ye"},
                {'ж', "zh"}, {'з', "z"}, {'и', "i"}, {'і', "i"}, {'ї', "yi"}, {'й', "y"}, {'к', "k"},
                {'л', "l"}, {'м', "m"}, {'н', "n"}, {'о', "o"}, {'п', "p"}, {'р', "r"}, {'с', "s"},
                {'т', "t"}, {'у', "u"}, {'ф', "f"}, {'х', "kh"}, {'ц', "ts"}, {'ч', "ch"}, {'ш', "sh"},
                {'щ', "shch"}, {'ь', ""}, {'ю', "yu"}, {'я', "ya"},
                {'А', "A"}, {'Б', "B"}, {'В', "V"}, {'Г', "G"}, {'Д', "D"}, {'Е', "E"}, {'Є', "Ye"},
                {'Ж', "Zh"}, {'З', "Z"}, {'И', "I"}, {'І', "I"}, {'Ї', "Yi"}, {'Й', "Y"}, {'К', "K"},
                {'Л', "L"}, {'М', "M"}, {'Н', "N"}, {'О', "O"}, {'П', "P"}, {'Р', "R"}, {'С', "S"},
                {'Т', "T"}, {'У', "U"}, {'Ф', "F"}, {'Х', "Kh"}, {'Ц', "Ts"}, {'Ч', "Ch"}, {'Ш', "Sh"},
                {'Щ', "Shch"}, {'Ь', ""}, {'Ю', "Yu"}, {'Я', "Ya"}
            };

            var result = text.ToLower();
            foreach (var kvp in transliteration)
            {
                result = result.Replace(kvp.Key.ToString(), kvp.Value);
            }

            // Удаление всех символов кроме букв, цифр и дефисов
            result = Regex.Replace(result, @"[^a-z0-9\-]", "-");
            
            // Удаление множественных дефисов
            result = Regex.Replace(result, @"-+", "-");
            
            // Удаление дефисов в начале и конце
            result = result.Trim('-');

            return result;
        }
    }

    public class GenerateSlugRequest
    {
        public string Text { get; set; } = string.Empty;
    }
}
