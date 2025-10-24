using ASP_421.Data.Entities;

namespace ASP_421.Services.Interfaces
{
    public interface IProductGroupService
    {
        Task<IEnumerable<ProductGroup>> GetAllGroupsAsync();
        Task<IEnumerable<ProductGroup>> GetParentGroupsAsync();
        Task<IEnumerable<ProductGroup>> GetSubGroupsAsync(Guid parentId);
        Task<int> GetSubGroupsCountAsync(Guid parentId);
        Task<ProductGroup?> GetGroupByIdAsync(Guid id);
        Task<ProductGroup?> GetGroupBySlugAsync(string slug);
        Task CreateGroupAsync(ProductGroup group);
        Task UpdateGroupAsync(ProductGroup group);
        Task SoftDeleteGroupAsync(Guid groupId);
        Task<bool> IsGroupSlugUniqueAsync(string slug, Guid? excludeId = null);
        Task ClearCacheAsync();
    }
}
