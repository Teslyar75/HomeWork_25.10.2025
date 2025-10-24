using ASP_421.Data.Entities;

namespace ASP_421.Services.Interfaces
{
    public interface ICartService
    {
        Task<IEnumerable<CartItem>> GetUserCartItemsAsync(Guid userId);
        Task<CartItem?> GetCartItemAsync(Guid userId, Guid productId);
        Task AddToCartAsync(Guid userId, Guid productId, int quantity = 1);
        Task UpdateCartItemQuantityAsync(Guid userId, Guid productId, int quantity);
        Task ModifyCartItemAsync(Guid userId, Guid productId, int increment);
        Task RemoveFromCartAsync(Guid userId, Guid productId);
        Task ClearCartAsync(Guid userId);
        Task<int> GetCartItemsCountAsync(Guid userId);
        Task<decimal> GetCartTotalAsync(Guid userId);
    }
}
