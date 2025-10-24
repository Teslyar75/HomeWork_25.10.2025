using ASP_421.Data.Entities;

namespace ASP_421.Services.Interfaces
{
    public interface IProductService
    {
        Task<Product?> GetProductByIdAsync(Guid id);
        Task<Product?> GetProductBySlugAsync(string slug);
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<IEnumerable<Product>> GetProductsByGroupAsync(Guid groupId);
        Task<IEnumerable<Product>> GetRelatedProductsAsync(Guid productId, int count = 6);
        Task CreateProductAsync(Product product);
        Task UpdateProductAsync(Product product);
        Task SoftDeleteProductAsync(Guid productId);
        Task<bool> IsProductNameUniqueInGroupAsync(string name, Guid groupId, Guid? excludeId = null);
        Task<bool> IsProductSlugUniqueAsync(string slug, Guid? excludeId = null);
    }
}
