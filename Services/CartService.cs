using ASP_421.Data;
using ASP_421.Data.Entities;
using ASP_421.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ASP_421.Services
{
    public class CartService : ICartService
    {
        private readonly DataContext _dataContext;

        public CartService(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<IEnumerable<CartItem>> GetUserCartItemsAsync(Guid userId)
        {
            return await _dataContext.CartItems
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .ThenInclude(p => p.Group)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<CartItem?> GetCartItemAsync(Guid userId, Guid productId)
        {
            return await _dataContext.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);
        }

        public async Task AddToCartAsync(Guid userId, Guid productId, int quantity = 1)
        {
            // Валидация количества
            if (quantity <= 0)
            {
                throw new ArgumentException("Кількість товару повинна бути більше нуля");
            }

            // Получаем товар для проверки складских остатков
            var product = await _dataContext.Products
                .FirstOrDefaultAsync(p => p.Id == productId && p.DeletedAt == null);
            
            if (product == null)
            {
                throw new ArgumentException("Товар не знайдено");
            }

            var existingItem = await GetCartItemAsync(userId, productId);
            var newQuantity = existingItem != null ? existingItem.Quantity + quantity : quantity;
            
            // Проверяем, не превышает ли общее количество складские остатки
            if (newQuantity > product.Stock)
            {
                throw new InvalidOperationException($"Недостатньо товару на складі. Доступно: {product.Stock}, запитується: {newQuantity}");
            }
            
            if (existingItem != null)
            {
                // Если товар уже есть в корзине, увеличиваем количество
                existingItem.Quantity = newQuantity;
                existingItem.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Создаем новый элемент корзины
                var cartItem = new CartItem
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity,
                    AddedAt = DateTime.UtcNow
                };
                _dataContext.CartItems.Add(cartItem);
            }
            
            await _dataContext.SaveChangesAsync();
        }

        public async Task UpdateCartItemQuantityAsync(Guid userId, Guid productId, int quantity)
        {
            // Валидация количества
            if (quantity < 0)
            {
                throw new ArgumentException("Кількість товару не може бути від'ємною");
            }

            var cartItem = await GetCartItemAsync(userId, productId);
            if (cartItem != null)
            {
                if (quantity == 0)
                {
                    // Если количество равно нулю, удаляем товар из корзины
                    await RemoveFromCartAsync(userId, productId);
                }
                else
                {
                    // Получаем товар для проверки складских остатков
                    var product = await _dataContext.Products
                        .FirstOrDefaultAsync(p => p.Id == productId && p.DeletedAt == null);
                    
                    if (product == null)
                    {
                        throw new ArgumentException("Товар не знайдено");
                    }

                    // Проверяем, не превышает ли новое количество складские остатки
                    if (quantity > product.Stock)
                    {
                        throw new InvalidOperationException($"Недостатньо товару на складі. Доступно: {product.Stock}, запитується: {quantity}");
                    }

                    cartItem.Quantity = quantity;
                    cartItem.UpdatedAt = DateTime.UtcNow;
                    await _dataContext.SaveChangesAsync();
                }
            }
        }

        public async Task ModifyCartItemAsync(Guid userId, Guid productId, int increment)
        {
            var cartItem = await GetCartItemAsync(userId, productId);
            if (cartItem != null)
            {
                var newQuantity = cartItem.Quantity + increment;
                
                // Если новое количество меньше нуля, выбрасываем исключение
                if (newQuantity < 0)
                {
                    throw new ArgumentException("Кількість товару не може бути від'ємною");
                }
                
                // Если новое количество равно нулю, удаляем товар
                if (newQuantity == 0)
                {
                    await RemoveFromCartAsync(userId, productId);
                    return;
                }
                
                // Получаем товар для проверки складских остатков
                var product = await _dataContext.Products
                    .FirstOrDefaultAsync(p => p.Id == productId && p.DeletedAt == null);
                
                if (product == null)
                {
                    throw new ArgumentException("Товар не знайдено");
                }

                // Проверяем, не превышает ли новое количество складские остатки
                if (newQuantity > product.Stock)
                {
                    throw new InvalidOperationException($"Недостатньо товару на складі. Доступно: {product.Stock}, запитується: {newQuantity}");
                }

                cartItem.Quantity = newQuantity;
                cartItem.UpdatedAt = DateTime.UtcNow;
                await _dataContext.SaveChangesAsync();
            }
        }

        public async Task RemoveFromCartAsync(Guid userId, Guid productId)
        {
            var cartItem = await GetCartItemAsync(userId, productId);
            if (cartItem != null)
            {
                _dataContext.CartItems.Remove(cartItem);
                await _dataContext.SaveChangesAsync();
            }
        }

        public async Task ClearCartAsync(Guid userId)
        {
            var cartItems = await _dataContext.CartItems
                .Where(c => c.UserId == userId)
                .ToListAsync();
            
            _dataContext.CartItems.RemoveRange(cartItems);
            await _dataContext.SaveChangesAsync();
        }

        public async Task<int> GetCartItemsCountAsync(Guid userId)
        {
            return await _dataContext.CartItems
                .CountAsync(c => c.UserId == userId);
        }

        public async Task<decimal> GetCartTotalAsync(Guid userId)
        {
            return await _dataContext.CartItems
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .SumAsync(c => c.Quantity * c.Product.Price);
        }
    }
}
