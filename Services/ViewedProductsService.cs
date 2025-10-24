using ASP_421.Data;
using ASP_421.Data.Entities;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace ASP_421.Services
{
    public interface IViewedProductsService
    {
        void AddViewedProduct(string sessionId, Guid productId);
        IEnumerable<Product> GetViewedProducts(string sessionId, int maxItems = 4);
        void ClearViewedProducts(string sessionId);
    }

    public class ViewedProductsService : IViewedProductsService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly DataAccessor _dataAccessor;
        private const string ViewedProductsSessionKey = "ViewedProducts";

        public ViewedProductsService(IHttpContextAccessor httpContextAccessor, DataAccessor dataAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _dataAccessor = dataAccessor;
        }

        public void AddViewedProduct(string sessionId, Guid productId)
        {
            Console.WriteLine($"ViewedProductsService.AddViewedProduct - SessionId: {sessionId}, ProductId: {productId}");
            
            if (string.IsNullOrEmpty(sessionId) || productId == Guid.Empty)
            {
                Console.WriteLine("ViewedProductsService.AddViewedProduct - Invalid sessionId or productId");
                return;
            }

            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null)
            {
                Console.WriteLine("ViewedProductsService.AddViewedProduct - Session is null");
                return;
            }

            // Получаем существующий список просмотренных товаров из сессии
            var viewedProductsJson = session.GetString(ViewedProductsSessionKey);
            var viewedProductIds = new List<Guid>();
            
            if (!string.IsNullOrEmpty(viewedProductsJson))
            {
                try
                {
                    viewedProductIds = JsonConvert.DeserializeObject<List<Guid>>(viewedProductsJson) ?? new List<Guid>();
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"ViewedProductsService.AddViewedProduct - JSON deserialize error: {ex.Message}");
                    viewedProductIds = new List<Guid>();
                }
            }

            Console.WriteLine($"ViewedProductsService.AddViewedProduct - Current viewed products count: {viewedProductIds.Count}");
            
            // Удаляем товар, если он уже есть в списке
            viewedProductIds.Remove(productId);
            
            // Добавляем товар в начало списка
            viewedProductIds.Insert(0, productId);
            
            Console.WriteLine($"ViewedProductsService.AddViewedProduct - Added product {productId} to session {sessionId}. Total items: {viewedProductIds.Count}");
            
            // Ограничиваем количество просмотренных товаров
            if (viewedProductIds.Count > 10)
            {
                viewedProductIds.RemoveRange(10, viewedProductIds.Count - 10);
                Console.WriteLine($"ViewedProductsService.AddViewedProduct - Trimmed to 10 items");
            }

            // Сохраняем обновленный список в сессию
            var updatedJson = JsonConvert.SerializeObject(viewedProductIds);
            session.SetString(ViewedProductsSessionKey, updatedJson);
            
            Console.WriteLine($"ViewedProductsService.AddViewedProduct - Saved to session: {updatedJson}");
        }

        public IEnumerable<Product> GetViewedProducts(string sessionId, int maxItems = 4)
        {
            Console.WriteLine($"ViewedProductsService.GetViewedProducts - SessionId: {sessionId}, MaxItems: {maxItems}");
            
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null)
            {
                Console.WriteLine("ViewedProductsService.GetViewedProducts - Session is null");
                return new List<Product>();
            }

            // Получаем список просмотренных товаров из сессии
            var viewedProductsJson = session.GetString(ViewedProductsSessionKey);
            if (string.IsNullOrEmpty(viewedProductsJson))
            {
                Console.WriteLine($"ViewedProductsService.GetViewedProducts - No viewed products in session: {sessionId}");
                return new List<Product>();
            }

            List<Guid> viewedProductIds;
            try
            {
                viewedProductIds = JsonConvert.DeserializeObject<List<Guid>>(viewedProductsJson) ?? new List<Guid>();
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"ViewedProductsService.GetViewedProducts - JSON deserialize error: {ex.Message}");
                return new List<Product>();
            }

            Console.WriteLine($"ViewedProductsService.GetViewedProducts - Found {viewedProductIds.Count} viewed product IDs");
            
            if (!viewedProductIds.Any())
            {
                Console.WriteLine("ViewedProductsService.GetViewedProducts - No viewed product IDs");
                return new List<Product>();
            }

            var limitedProductIds = viewedProductIds.Take(maxItems).ToList();

            // Получаем товары из всех групп базы данных
            var products = _dataAccessor.ProductGroups()
                .SelectMany(g => g.Products.Where(p => p.DeletedAt == null && limitedProductIds.Contains(p.Id)))
                .OrderBy(p => limitedProductIds.IndexOf(p.Id))
                .ToList();

            Console.WriteLine($"ViewedProductsService.GetViewedProducts - Retrieved {products.Count} products from database");
            foreach (var product in products)
            {
                Console.WriteLine($"ViewedProductsService.GetViewedProducts - Product: {product.Name}, Group: {product.Group?.Name}");
            }

            return products;
        }

        public void ClearViewedProducts(string sessionId)
        {
            Console.WriteLine($"ViewedProductsService.ClearViewedProducts - SessionId: {sessionId}");
            
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null)
            {
                Console.WriteLine("ViewedProductsService.ClearViewedProducts - Session is null");
                return;
            }

            session.Remove(ViewedProductsSessionKey);
            Console.WriteLine("ViewedProductsService.ClearViewedProducts - Cleared viewed products from session");
        }
    }
}
