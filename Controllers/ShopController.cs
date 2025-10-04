using ASP_421.Data;
using ASP_421.Models.Shop;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ASP_421.Controllers
{
    public class ShopController(DataAccessor dataAccessor) : Controller
    {
        private readonly DataAccessor _dataAccessor = dataAccessor;

        public IActionResult Index()
        {
            // Получаем родительские группы с загруженными товарами
            var groups = _dataAccessor.ProductGroups()
                .Where(g => g.DeletedAt == null && g.ParentId == null)
                .ToList(); // Materialize to load Products
            
            ShopIndexViewModel model = new()
            {
                ProductGroups = groups
            };
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
                        ProductGroups = _dataAccessor.ProductGroups()
                    };
                    return View(model);
                }
            }
            return RedirectToAction(nameof(Index));
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
