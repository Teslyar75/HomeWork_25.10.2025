using ASP_421.Data.Entities;
using ASP_421.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ASP_421.Services
{
    public class CachedProductGroupService : IProductGroupService
    {
        private readonly ProductGroupService _productGroupService;
        private readonly ICacheService _cacheService;
        private const string CachePrefix = "ProductGroup_";
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);

        public CachedProductGroupService(ProductGroupService productGroupService, ICacheService cacheService)
        {
            _productGroupService = productGroupService;
            _cacheService = cacheService;
        }

        public async Task<IEnumerable<ProductGroup>> GetAllGroupsAsync()
        {
            const string cacheKey = $"{CachePrefix}All";
            var cached = await _cacheService.GetAsync<IEnumerable<ProductGroup>>(cacheKey);
            
            if (cached != null)
            {
                return cached;
            }

            var result = await _productGroupService.GetAllGroupsAsync();
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);
            return result;
        }

        public async Task<IEnumerable<ProductGroup>> GetParentGroupsAsync()
        {
            const string cacheKey = $"{CachePrefix}Parent";
            var cached = await _cacheService.GetAsync<IEnumerable<ProductGroup>>(cacheKey);
            
            if (cached != null)
            {
                return cached;
            }

            var result = await _productGroupService.GetParentGroupsAsync();
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);
            return result;
        }

        public async Task<IEnumerable<ProductGroup>> GetSubGroupsAsync(Guid parentId)
        {
            var cacheKey = $"{CachePrefix}Sub_{parentId}";
            var cached = await _cacheService.GetAsync<IEnumerable<ProductGroup>>(cacheKey);
            
            if (cached != null)
            {
                return cached;
            }

            var result = await _productGroupService.GetSubGroupsAsync(parentId);
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);
            return result;
        }

        public async Task<int> GetSubGroupsCountAsync(Guid parentId)
        {
            var cacheKey = $"{CachePrefix}SubCount_{parentId}";
            var cached = await _cacheService.GetAsync<int?>(cacheKey);
            
            if (cached.HasValue)
            {
                return cached.Value;
            }

            var result = await _productGroupService.GetSubGroupsCountAsync(parentId);
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);
            return result;
        }

        public async Task<ProductGroup?> GetGroupByIdAsync(Guid id)
        {
            var cacheKey = $"{CachePrefix}ById_{id}";
            var cached = await _cacheService.GetAsync<ProductGroup?>(cacheKey);
            
            if (cached != null)
            {
                return cached;
            }

            var result = await _productGroupService.GetGroupByIdAsync(id);
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);
            return result;
        }

        public async Task<ProductGroup?> GetGroupBySlugAsync(string slug)
        {
            var cacheKey = $"{CachePrefix}BySlug_{slug}";
            var cached = await _cacheService.GetAsync<ProductGroup?>(cacheKey);
            
            if (cached != null)
            {
                return cached;
            }

            var result = await _productGroupService.GetGroupBySlugAsync(slug);
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);
            return result;
        }

        public async Task CreateGroupAsync(ProductGroup group)
        {
            await _productGroupService.CreateGroupAsync(group);
            await InvalidateCacheAsync();
        }

        public async Task UpdateGroupAsync(ProductGroup group)
        {
            await _productGroupService.UpdateGroupAsync(group);
            await InvalidateCacheAsync();
        }

        public async Task SoftDeleteGroupAsync(Guid groupId)
        {
            await _productGroupService.SoftDeleteGroupAsync(groupId);
            await InvalidateCacheAsync();
        }

        public async Task<bool> IsGroupSlugUniqueAsync(string slug, Guid? excludeId = null)
        {
            return await _productGroupService.IsGroupSlugUniqueAsync(slug, excludeId);
        }

        public async Task ClearCacheAsync()
        {
            await InvalidateCacheAsync();
        }

        private async Task InvalidateCacheAsync()
        {
            // Очищаем все кэшированные данные групп
            await _cacheService.RemoveAsync($"{CachePrefix}All");
            await _cacheService.RemoveAsync($"{CachePrefix}Parents");
            // Очищаем кэш для всех групп по slug
            var allGroups = await _productGroupService.GetAllGroupsAsync();
            foreach (var group in allGroups)
            {
                await _cacheService.RemoveAsync($"{CachePrefix}Slug_{group.Slug}");
            }
        }
    }
}
