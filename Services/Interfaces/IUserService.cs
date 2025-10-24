using ASP_421.Data.Entities;

namespace ASP_421.Services.Interfaces
{
    public interface IUserService
    {
        Task<User?> GetUserByLoginAsync(string login);
        Task<User?> GetUserByIdAsync(Guid id);
        Task<bool> IsEmailUniqueAsync(string email, Guid? excludeId = null);
        Task<bool> IsLoginUniqueAsync(string login);
        Task<User> CreateUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task SoftDeleteUserAsync(Guid userId);
        Task<IEnumerable<Order>> GetUserOrdersAsync(Guid userId);
        Task<IEnumerable<CartItem>> GetUserCartItemsAsync(Guid userId);
        Task<int> GetUserCartItemsCountAsync(Guid userId);
        Task<decimal> GetUserCartTotalAsync(Guid userId);
    }
}
