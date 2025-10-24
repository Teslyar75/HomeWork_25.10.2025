using ASP_421.Data;
using ASP_421.Data.Entities;
using ASP_421.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ASP_421.Services
{
    public class ProductGroupService : IProductGroupService
    {
        private readonly DataContext _dataContext;

        public ProductGroupService(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<IEnumerable<ProductGroup>> GetAllGroupsAsync()
        {
            return await _dataContext.ProductGroups
                .Where(g => g.DeletedAt == null)
                .Include(g => g.Products.Where(p => p.DeletedAt == null))
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<ProductGroup>> GetParentGroupsAsync()
        {
            return await _dataContext.ProductGroups
                .Where(g => g.DeletedAt == null && g.ParentId == null)
                .Include(g => g.Products.Where(p => p.DeletedAt == null))
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<ProductGroup>> GetSubGroupsAsync(Guid parentId)
        {
            return await _dataContext.ProductGroups
                .Where(g => g.DeletedAt == null && g.ParentId == parentId)
                .Include(g => g.Products.Where(p => p.DeletedAt == null))
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<int> GetSubGroupsCountAsync(Guid parentId)
        {
            return await _dataContext.ProductGroups
                .CountAsync(g => g.DeletedAt == null && g.ParentId == parentId);
        }

        public async Task<ProductGroup?> GetGroupByIdAsync(Guid id)
        {
            return await _dataContext.ProductGroups
                .Include(g => g.Products.Where(p => p.DeletedAt == null))
                .FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task<ProductGroup?> GetGroupBySlugAsync(string slug)
        {
            return await _dataContext.ProductGroups
                .Include(g => g.Products.Where(p => p.DeletedAt == null))
                .FirstOrDefaultAsync(g => g.Slug == slug && g.DeletedAt == null);
        }

        public async Task CreateGroupAsync(ProductGroup group)
        {
            // Проверяем уникальность slug
            if (!string.IsNullOrEmpty(group.Slug) && !await IsGroupSlugUniqueAsync(group.Slug))
            {
                throw new InvalidOperationException($"Група з таким slug '{group.Slug}' вже існує");
            }

            group.Id = Guid.NewGuid();
            group.CreatedAt = DateTime.UtcNow;
            
            _dataContext.ProductGroups.Add(group);
            await _dataContext.SaveChangesAsync();
        }

        public async Task UpdateGroupAsync(ProductGroup group)
        {
            var existingGroup = await _dataContext.ProductGroups.FindAsync(group.Id);
            if (existingGroup != null)
            {
                existingGroup.Name = group.Name;
                existingGroup.Description = group.Description;
                existingGroup.ImageUrl = group.ImageUrl;
                existingGroup.Slug = group.Slug;
                existingGroup.ParentId = group.ParentId;
                existingGroup.UpdatedAt = DateTime.UtcNow;
                
                await _dataContext.SaveChangesAsync();
            }
        }

        public async Task SoftDeleteGroupAsync(Guid groupId)
        {
            var group = await _dataContext.ProductGroups.FindAsync(groupId);
            if (group != null)
            {
                group.DeletedAt = DateTime.UtcNow;
                await _dataContext.SaveChangesAsync();
            }
        }

        public async Task<bool> IsGroupSlugUniqueAsync(string slug, Guid? excludeId = null)
        {
            var query = _dataContext.ProductGroups
                .Where(g => g.DeletedAt == null && g.Slug == slug);
            
            if (excludeId.HasValue)
            {
                query = query.Where(g => g.Id != excludeId.Value);
            }
            
            return !await query.AnyAsync();
        }

        public async Task ClearCacheAsync()
        {
            // Базовый сервис не использует кэш, поэтому ничего не делаем
            await Task.CompletedTask;
        }
    }
}
