using ASP_421.Data;
using ASP_421.Models.Shop;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ASP_421.Services;
using ASP_421.Services.Storage;

namespace ASP_421.Controllers
{
    public class ShopController(DataAccessor dataAccessor, IViewedProductsService viewedProductsService, IStorageService storageService) : Controller
    {
        private readonly DataAccessor _dataAccessor = dataAccessor;
        private readonly IViewedProductsService _viewedProductsService = viewedProductsService;
        private readonly IStorageService _storageService = storageService;

        public IActionResult Index()
        {
            // Получаем родительские группы с загруженными товарами
            var groups = _dataAccessor.ProductGroups()
                .Where(g => g.DeletedAt == null && g.ParentId == null)
                .ToList(); // Materialize to load Products
            
            // Получаем просмотренные товары из всех групп
            var sessionId = HttpContext.Session.Id;
            var viewedProducts = _viewedProductsService.GetViewedProducts(sessionId, 8);
            
            ShopIndexViewModel model = new()
            {
                ProductGroups = groups
            };
            
            ViewData["ViewedProducts"] = viewedProducts;
            return View(model);
        }

        public IActionResult Group(String id)
        {
            // Находим группу по slug с загруженными товарами
            var group = _dataAccessor.ProductGroups()
                .FirstOrDefault(g => g.DeletedAt == null && g.Slug == id);

            if (group == null)
            {
                return NotFound();
            }

            // Получаем товары этой группы
            var products = group.Products
                .Where(p => p.DeletedAt == null)
                .ToList();

            ViewData["Group"] = group;
            ViewData["Products"] = products;
            ViewData["id"] = id;
            
            return View();
        }

        public IActionResult Product(String slug)
        {
            // Находим товар по slug
            var product = _dataAccessor.ProductGroups()
                .SelectMany(g => g.Products.Where(p => p.DeletedAt == null))
                .FirstOrDefault(p => p.Slug == slug);

            if (product == null)
            {
                return NotFound();
            }

            // Добавляем товар в просмотренные
            var sessionId = HttpContext.Session.Id;
            _viewedProductsService.AddViewedProduct(sessionId, product.Id);

            // Получаем похожие товары: 3 из той же группы + 3 из других групп
            var sameGroupProducts = product.Group.Products
                .Where(p => p.DeletedAt == null && p.Id != product.Id)
                .OrderBy(x => Guid.NewGuid()) // Случайный порядок
                .Take(3)
                .ToList();

            // Получаем товары из других групп (случайно)
            var otherGroupsProducts = _dataAccessor.ProductGroups()
                .Where(g => g.DeletedAt == null && g.Id != product.GroupId)
                .SelectMany(g => g.Products.Where(p => p.DeletedAt == null))
                .OrderBy(x => Guid.NewGuid()) // Случайный порядок
                .Take(3)
                .ToList();

            // Объединяем: 3 из той же группы + 3 из других групп = 6 товаров
            var relatedProducts = sameGroupProducts.Concat(otherGroupsProducts).ToList();

            // Получаем просмотренные товары из всех групп
            var viewedProducts = _viewedProductsService.GetViewedProducts(sessionId, 8);

            ViewData["Product"] = product;
            ViewData["RelatedProducts"] = relatedProducts;
            ViewData["ViewedProducts"] = viewedProducts;
            
            return View();
        }

        public IActionResult Admin()
        {
            if(HttpContext.User.Identity?.IsAuthenticated ?? false)
            {
                String? role = HttpContext.User.Claims
                    .FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

                if (role == "Admin")
                {
                    ShopAdminViewModel model = new()
                    {
                        ProductGroups = _dataAccessor.ProductGroups(),
                        Products = _dataAccessor.GetAllProducts()
                    };
                    return View(model);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        public IActionResult AdminEdit()
        {
            if(HttpContext.User.Identity?.IsAuthenticated ?? false)
            {
                String? role = HttpContext.User.Claims
                    .FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

                if (role == "Admin")
                {
                    var model = new AdminEditViewModel
                    {
                        Groups = _dataAccessor.GetAllGroups()
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
                            .ToList(),
                        Products = _dataAccessor.GetAllProducts()
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
                            .ToList(),
                        AllGroups = _dataAccessor.GetAllGroups()
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
                            .ToList()
                    };
                    return View(model);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult UpdateProduct(Guid id, [FromForm] EditProductViewModel model, IFormFile? productImage)
        {
            Console.WriteLine($"POST request received for product ID: {id}");
            Console.WriteLine($"Request content type: {HttpContext.Request.ContentType}");
            
            // Проверка авторизации
            Console.WriteLine($"User authenticated: {HttpContext.User.Identity?.IsAuthenticated}");
            Console.WriteLine($"User name: {HttpContext.User.Identity?.Name}");
            
            if (!HttpContext.User.Identity?.IsAuthenticated ?? true)
            {
                Console.WriteLine("User not authenticated, returning 401");
                return Json(new { success = false, message = "Необхідна авторизація" });
            }
            
            var role = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            Console.WriteLine($"User role: {role}");
            
            if (role != "Admin")
            {
                Console.WriteLine("User is not admin, returning 403");
                return Json(new { success = false, message = "Доступ заборонено" });
            }

            try
            {
                Console.WriteLine($"Attempting to update product with ID: {id}");
                Console.WriteLine($"Model data - Name: '{model.Name}', Price: {model.Price}, Stock: {model.Stock}, GroupId: {model.GroupId}, ImageUrl: '{model.ImageUrl}'");
                Console.WriteLine($"Model data - Name length: {model.Name?.Length}, Price type: {model.Price.GetType()}");
                Console.WriteLine($"ProductImage: {(productImage != null ? $"File: {productImage.FileName}, Size: {productImage.Length}" : "null")}");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                    Console.WriteLine($"Validation errors: {string.Join(", ", errors)}");
                    foreach (var key in ModelState.Keys)
                    {
                        var state = ModelState[key];
                        if (state.Errors.Count > 0)
                        {
                            Console.WriteLine($"ModelState error for '{key}': {string.Join(", ", state.Errors.Select(e => e.ErrorMessage))}");
                        }
                    }
                    return Json(new { success = false, message = "Помилка валідації", errors = errors.ToList() });
                }

                var product = _dataAccessor.GetProductById(id);
                if (product == null)
                {
                    Console.WriteLine($"Product with ID {id} not found");
                    return Json(new { success = false, message = "Товар не знайдено" });
                }

                Console.WriteLine($"Found product: {product.Name}");

                // Обновляем данные товара
                product.Name = model.Name;
                product.Description = model.Description;
                product.Price = model.Price;
                product.Stock = model.Stock;
                product.GroupId = model.GroupId;
                product.Slug = model.Slug;

                // Обработка изображения
                if (productImage != null && productImage.Length > 0)
                {
                    Console.WriteLine($"Saving new image: {productImage.FileName}");
                    var imageUrl = _storageService.Save(productImage);
                    product.ImageUrl = imageUrl;
                    Console.WriteLine($"Image saved with URL: {imageUrl}");
                }
                else
                {
                    product.ImageUrl = model.ImageUrl; // Сохраняем старое изображение, если новое не загружено
                }

                _dataAccessor.UpdateProduct(product);

                Console.WriteLine($"Product {product.Name} successfully updated");
                return Json(new { success = true, message = "Товар успішно оновлено" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating product: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Помилка оновлення товара: {ex.Message}" });
            }
        }
    }
}
/* Д.З. Реалізувати валідацію моделі форми товару (адмінка),
 * що передається на додавання до БД. 
 * За відсутності помилок очищувати форму від введених даних.
 * 
 * Додати до форми створення нової групи поле з введенням
 * батьківської групи (для створення підгруп), доповнити валідацію
 * моделі групи.
 * 
 * ** За зразком сайту Amazon додати до карточки групи (на домашній
 * сторінці) відомості про підгрупи (або виводити текстом їх кількість
 * або формувати зображення з зображень перших підгруп)
 */
