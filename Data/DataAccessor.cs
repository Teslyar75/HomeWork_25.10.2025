using ASP_421.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ASP_421.Data
{
    public class DataAccessor(DataContext dataContext)
    {
        private readonly DataContext _dataContext = dataContext;

        public void AddProduct(Product product)
        {
            if(product.Id == default)
            {
                product.Id = Guid.NewGuid();
            }
            product.DeletedAt = null;
            _dataContext.Products.Add(product);
            _dataContext.SaveChanges();
        }

        public void AddProductGroup(ProductGroup group)
        {
            if(group.Id == default)
            {
                group.Id = Guid.NewGuid();
            }
            group.DeletedAt = null;
            _dataContext.ProductGroups.Add(group);
            _dataContext.SaveChanges();
        }

        public IEnumerable<ProductGroup> ProductGroups()
        {
            return _dataContext
                .ProductGroups
                .Where(g => g.DeletedAt == null)
                .Include(g => g.Products.Where(p => p.DeletedAt == null))
                .AsEnumerable();
        }

        public IEnumerable<ProductGroup> GetParentGroups()
        {
            return _dataContext
                .ProductGroups
                .Where(g => g.DeletedAt == null && g.ParentId == null)
                .AsEnumerable();
        }

        public IEnumerable<ProductGroup> GetSubGroups(Guid parentId)
        {
            return _dataContext
                .ProductGroups
                .Where(g => g.DeletedAt == null && g.ParentId == parentId)
                .AsEnumerable();
        }

        public int GetSubGroupsCount(Guid parentId)
        {
            return _dataContext
                .ProductGroups
                .Count(g => g.DeletedAt == null && g.ParentId == parentId);
        }

    public bool IsProductSlugUnique(string slug)
    {
        return !_dataContext.Products.Any(p => p.DeletedAt == null && p.Slug == slug);
    }
}
}
