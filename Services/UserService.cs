using ASP_421.Data;
using ASP_421.Data.Entities;
using ASP_421.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ASP_421.Services
{
    public class UserService : IUserService
    {
        private readonly DataContext _dataContext;

        public UserService(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<User?> GetUserByLoginAsync(string login)
        {
            return await _dataContext.UserAccesses
                .Include(ua => ua.User)
                .Where(ua => ua.Login == login && ua.User.DeletedAt == null)
                .Select(ua => ua.User)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            return await _dataContext.Users
                .Where(u => u.Id == id && u.DeletedAt == null)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task<bool> IsEmailUniqueAsync(string email, Guid? excludeId = null)
        {
            var query = _dataContext.Users
                .Where(u => u.Email == email && u.DeletedAt == null);
            
            if (excludeId.HasValue)
            {
                query = query.Where(u => u.Id != excludeId.Value);
            }
            
            return !await query.AnyAsync();
        }

        public async Task<bool> IsLoginUniqueAsync(string login)
        {
            return !await _dataContext.UserAccesses
                .AnyAsync(ua => ua.Login == login);
        }

        public async Task<User> CreateUserAsync(User user)
        {
            user.Id = Guid.NewGuid();
            user.RegisterDt = DateTime.UtcNow;
            
            _dataContext.Users.Add(user);
            await _dataContext.SaveChangesAsync();
            
            return user;
        }

        public async Task UpdateUserAsync(User user)
        {
            var existingUser = await _dataContext.Users.FindAsync(user.Id);
            if (existingUser != null)
            {
                existingUser.Name = user.Name;
                existingUser.Email = user.Email;
                existingUser.Avatar = user.Avatar;
                existingUser.UpdatedAt = DateTime.UtcNow;
                
                await _dataContext.SaveChangesAsync();
            }
        }

        public async Task SoftDeleteUserAsync(Guid userId)
        {
            var user = await _dataContext.Users.FindAsync(userId);
            if (user != null)
            {
                user.DeletedAt = DateTime.UtcNow;
                await _dataContext.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Order>> GetUserOrdersAsync(Guid userId)
        {
            return await _dataContext.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                .AsNoTracking()
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
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

        public async Task<int> GetUserCartItemsCountAsync(Guid userId)
        {
            return await _dataContext.CartItems
                .CountAsync(c => c.UserId == userId);
        }

        public async Task<decimal> GetUserCartTotalAsync(Guid userId)
        {
            return await _dataContext.CartItems
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .SumAsync(c => c.Quantity * c.Product.Price);
        }
    }
}
