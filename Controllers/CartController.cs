using ASP_421.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ASP_421.Services;

namespace ASP_421.Controllers
{
    public class CartController(DataAccessor dataAccessor, IViewedProductsService viewedProductsService) : Controller
    {
        private readonly DataAccessor _dataAccessor = dataAccessor;
        private readonly IViewedProductsService _viewedProductsService = viewedProductsService;

        private Guid? GetCurrentUserId()
        {
            // Сначала пробуем найти claim с типом "Id" (как в UserController)
            var userIdClaim = User.FindFirst("Id")?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            
            // Если не найден, пробуем ClaimTypes.NameIdentifier
            userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out userId) ? userId : null;
        }

        public IActionResult Index()
        {
            var userId = GetCurrentUserId();
            
            // Логируем информацию о пользователе для диагностики
            Console.WriteLine($"Cart Controller - User claims: {string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
            Console.WriteLine($"Cart Controller - Extracted userId: {userId}");
            
            if (userId == null)
            {
                // Для неавторизованных пользователей перенаправляем на страницу регистрации
                return RedirectToAction("SignUp", "User");
            }

            var cartItems = _dataAccessor.GetUserCartItems(userId.Value);
            var totalAmount = _dataAccessor.GetCartTotal(userId.Value);

            // Получаем рекомендуемые товары (случайные товары, которых нет в корзине)
            var cartProductIds = cartItems.Select(ci => ci.ProductId).ToList();
            var recommendedProducts = _dataAccessor.ProductGroups()
                .SelectMany(g => g.Products.Where(p => p.DeletedAt == null && !cartProductIds.Contains(p.Id)))
                .OrderBy(x => Guid.NewGuid()) // Случайный порядок
                .Take(8)
                .ToList();

            // Диагностика: логируем информацию о рекомендуемых товарах
            Console.WriteLine($"Recommended products count: {recommendedProducts.Count}");
            foreach (var product in recommendedProducts)
            {
                Console.WriteLine($"Product: {product.Name}, ImageUrl: '{product.ImageUrl}', Group: {product.Group?.Name}");
            }

            // Получаем просмотренные товары из всех групп
            var sessionId = HttpContext.Session.Id;
            var viewedProducts = _viewedProductsService.GetViewedProducts(sessionId, 8);

            ViewData["CartItems"] = cartItems;
            ViewData["TotalAmount"] = totalAmount;
            ViewData["ItemsCount"] = cartItems.Count();
            ViewData["RecommendedProducts"] = recommendedProducts;
            ViewData["ViewedProducts"] = viewedProducts;

            return View();
        }
    }
}
