using ASP_421.Data;
using ASP_421.Data.Entities;
using ASP_421.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ASP_421.Services
{
    public class ProductService : IProductService
    {
        private readonly DataContext _dataContext;

        public ProductService(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<Product?> GetProductByIdAsync(Guid id)
        {
            return await _dataContext.Products
                .Include(p => p.Group)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Product?> GetProductBySlugAsync(string slug)
        {
            return await _dataContext.Products
                .Include(p => p.Group)
                .FirstOrDefaultAsync(p => p.Slug == slug && p.DeletedAt == null);
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _dataContext.Products
                .Where(p => p.DeletedAt == null)
                .Include(p => p.Group)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByGroupAsync(Guid groupId)
        {
            return await _dataContext.Products
                .Where(p => p.GroupId == groupId && p.DeletedAt == null)
                .Include(p => p.Group)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetRelatedProductsAsync(Guid productId, int count = 6)
        {
            var product = await GetProductByIdAsync(productId);
            if (product == null) return new List<Product>();

            // Получаем товары из той же группы (исключая текущий)
            var sameGroupProducts = await _dataContext.Products
                .Where(p => p.GroupId == product.GroupId && p.DeletedAt == null && p.Id != productId)
                .AsNoTracking()
                .ToListAsync();

            // Получаем товары из других групп
            var otherGroupsProducts = await _dataContext.ProductGroups
                .Where(g => g.DeletedAt == null && g.Id != product.GroupId)
                .SelectMany(g => g.Products.Where(p => p.DeletedAt == null))
                .AsNoTracking()
                .ToListAsync();

            // Перемешиваем на клиенте
            var random = new System.Random();
            var shuffledSameGroup = sameGroupProducts.OrderBy(x => random.Next()).Take(3);
            var shuffledOtherGroups = otherGroupsProducts.OrderBy(x => random.Next()).Take(3);

            return shuffledSameGroup.Concat(shuffledOtherGroups).ToList();
        }

        public async Task CreateProductAsync(Product product)
        {
            // Проверяем уникальность slug
            if (!string.IsNullOrEmpty(product.Slug) && !await IsProductSlugUniqueAsync(product.Slug))
            {
                throw new InvalidOperationException($"Товар з таким slug '{product.Slug}' вже існує");
            }

            product.Id = Guid.NewGuid();
            product.CreatedAt = DateTime.UtcNow;
            
            _dataContext.Products.Add(product);
            await _dataContext.SaveChangesAsync();
        }

        public async Task UpdateProductAsync(Product product)
        {
            var existingProduct = await _dataContext.Products.FindAsync(product.Id);
            if (existingProduct != null)
            {
                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.Stock = product.Stock;
                existingProduct.ImageUrl = product.ImageUrl;
                existingProduct.GroupId = product.GroupId;
                existingProduct.Slug = product.Slug;
                existingProduct.UpdatedAt = DateTime.UtcNow;
                
                await _dataContext.SaveChangesAsync();
            }
        }

        public async Task SoftDeleteProductAsync(Guid productId)
        {
            var product = await _dataContext.Products.FindAsync(productId);
            if (product != null)
            {
                product.DeletedAt = DateTime.UtcNow;
                await _dataContext.SaveChangesAsync();
            }
        }

        public async Task<bool> IsProductNameUniqueInGroupAsync(string name, Guid groupId, Guid? excludeId = null)
        {
            var query = _dataContext.Products
                .Where(p => p.DeletedAt == null && p.Name == name && p.GroupId == groupId);
            
            if (excludeId.HasValue)
            {
                query = query.Where(p => p.Id != excludeId.Value);
            }
            
            return !await query.AnyAsync();
        }

        public async Task<bool> IsProductSlugUniqueAsync(string slug, Guid? excludeId = null)
        {
            var query = _dataContext.Products
                .Where(p => p.DeletedAt == null && p.Slug == slug);
            
            if (excludeId.HasValue)
            {
                query = query.Where(p => p.Id != excludeId.Value);
            }
            
            return !await query.AnyAsync();
        }
    }
}
