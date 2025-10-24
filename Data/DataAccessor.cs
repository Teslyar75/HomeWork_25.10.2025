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
            
            // Проверка уникальности slug
            if (!IsProductSlugUnique(product.Slug))
            {
                throw new InvalidOperationException($"Товар з slug '{product.Slug}' вже існує");
            }
            
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


    // Методы для работы с корзиной
    public IEnumerable<CartItem> GetUserCartItems(Guid userId)
    {
        return _dataContext.CartItems
            .Where(c => c.UserId == userId)
            .Include(c => c.Product)
            .ThenInclude(p => p.Group)
            .AsEnumerable();
    }

    public CartItem? GetCartItem(Guid userId, Guid productId)
    {
        return _dataContext.CartItems
            .FirstOrDefault(c => c.UserId == userId && c.ProductId == productId);
    }

    public void AddToCart(Guid userId, Guid productId, int quantity = 1)
    {
        var existingItem = GetCartItem(userId, productId);
        
        if (existingItem != null)
        {
            // Если товар уже есть в корзине, увеличиваем количество
            existingItem.Quantity += quantity;
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
        
        _dataContext.SaveChanges();
    }

    public void UpdateCartItemQuantity(Guid userId, Guid productId, int quantity)
    {
        var cartItem = GetCartItem(userId, productId);
        if (cartItem != null)
        {
            if (quantity <= 0)
            {
                RemoveFromCart(userId, productId);
            }
            else
            {
                cartItem.Quantity = quantity;
                cartItem.UpdatedAt = DateTime.UtcNow;
                _dataContext.SaveChanges();
            }
        }
    }

    public void RemoveFromCart(Guid userId, Guid productId)
    {
        var cartItem = GetCartItem(userId, productId);
        if (cartItem != null)
        {
            _dataContext.CartItems.Remove(cartItem);
            _dataContext.SaveChanges();
        }
    }

    public void ClearCart(Guid userId)
    {
        var cartItems = _dataContext.CartItems.Where(c => c.UserId == userId);
        _dataContext.CartItems.RemoveRange(cartItems);
        _dataContext.SaveChanges();
    }

    public int GetCartItemsCount(Guid userId)
    {
        return _dataContext.CartItems.Count(c => c.UserId == userId);
    }

    public decimal GetCartTotal(Guid userId)
    {
        return _dataContext.CartItems
            .Where(c => c.UserId == userId)
            .Include(c => c.Product)
            .Sum(c => c.Quantity * c.Product.Price);
    }


    // Методы для редактирования групп
    public ProductGroup? GetGroupById(Guid id)
    {
        return _dataContext.ProductGroups
            .Include(g => g.Products.Where(p => p.DeletedAt == null))
            .FirstOrDefault(g => g.Id == id);
    }

    public void UpdateGroup(ProductGroup group)
    {
        var existingGroup = _dataContext.ProductGroups.Find(group.Id);
        if (existingGroup != null)
        {
            existingGroup.Name = group.Name;
            existingGroup.Description = group.Description;
            existingGroup.ImageUrl = group.ImageUrl;
            existingGroup.ParentId = group.ParentId;
            existingGroup.Slug = group.Slug;
            _dataContext.SaveChanges();
        }
    }

    public void SoftDeleteGroup(Guid groupId)
    {
        var group = _dataContext.ProductGroups.Find(groupId);
        if (group != null)
        {
            group.DeletedAt = DateTime.UtcNow;
            _dataContext.SaveChanges();
        }
    }

    public bool IsGroupSlugUnique(string slug, Guid? excludeId = null)
    {
        var query = _dataContext.ProductGroups.Where(g => g.DeletedAt == null && g.Slug == slug);
        if (excludeId.HasValue)
        {
            query = query.Where(g => g.Id != excludeId.Value);
        }
        return !query.Any();
    }

    // Методы для редактирования товаров
    public Product? GetProductById(Guid id)
    {
        return _dataContext.Products
            .Include(p => p.Group)
            .FirstOrDefault(p => p.Id == id);
    }

    public void UpdateProduct(Product product)
    {
        var existingProduct = _dataContext.Products.Find(product.Id);
        if (existingProduct != null)
        {
            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.Stock = product.Stock;
            existingProduct.ImageUrl = product.ImageUrl;
            existingProduct.GroupId = product.GroupId;
            existingProduct.Slug = product.Slug;
            _dataContext.SaveChanges();
        }
    }

    public void SoftDeleteProduct(Guid productId)
    {
        var product = _dataContext.Products.Find(productId);
        if (product != null)
        {
            product.DeletedAt = DateTime.UtcNow;
            _dataContext.SaveChanges();
        }
    }

    public bool IsProductNameUniqueInGroup(string name, Guid groupId, Guid? excludeId = null)
    {
        var query = _dataContext.Products.Where(p => p.DeletedAt == null && p.Name == name && p.GroupId == groupId);
        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }
        return !query.Any();
    }

    public bool IsProductSlugUnique(string slug, Guid? excludeId = null)
    {
        var query = _dataContext.Products.Where(p => p.DeletedAt == null && p.Slug == slug);
        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }
        return !query.Any();
    }

    // Получение всех групп для выпадающих списков
    public IEnumerable<ProductGroup> GetAllGroups()
    {
        return _dataContext.ProductGroups
            .Where(g => g.DeletedAt == null)
            .OrderBy(g => g.Name)
            .AsEnumerable();
    }

    // Получение всех товаров для админки
        public IEnumerable<Product> GetAllProducts()
        {
        return _dataContext.Products
            .Where(p => p.DeletedAt == null)
            .Include(p => p.Group)
            .OrderBy(p => p.Name)
            .AsEnumerable();
        }
    }
}
