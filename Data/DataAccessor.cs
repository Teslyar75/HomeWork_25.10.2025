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
            if (!string.IsNullOrEmpty(product.Slug) && !IsProductSlugUnique(product.Slug))
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
        // Валидация количества
        if (quantity <= 0)
        {
            throw new ArgumentException("Кількість товару повинна бути більше нуля");
        }

        // Получаем товар для проверки складских остатков
        var product = _dataContext.Products.FirstOrDefault(p => p.Id == productId && p.DeletedAt == null);
        if (product == null)
        {
            throw new ArgumentException("Товар не знайдено");
        }

        var existingItem = GetCartItem(userId, productId);
        var newQuantity = existingItem != null ? existingItem.Quantity + quantity : quantity;
        
        // Проверяем, не превышает ли общее количество складские остатки
        if (newQuantity > product.Stock)
        {
            throw new InvalidOperationException($"Недостатньо товару на складі. Доступно: {product.Stock}, запитується: {newQuantity}");
        }
        
        if (existingItem != null)
        {
            // Если товар уже есть в корзине, увеличиваем количество
            existingItem.Quantity = newQuantity;
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
        // Валидация количества
        if (quantity < 0)
        {
            throw new ArgumentException("Кількість товару не може бути від'ємною");
        }

        var cartItem = GetCartItem(userId, productId);
        if (cartItem != null)
        {
            if (quantity == 0)
            {
                // Если количество равно нулю, удаляем товар из корзины
                RemoveFromCart(userId, productId);
            }
            else
            {
                // Получаем товар для проверки складских остатков
                var product = _dataContext.Products.FirstOrDefault(p => p.Id == productId && p.DeletedAt == null);
                if (product == null)
                {
                    throw new ArgumentException("Товар не знайдено");
                }

                // Проверяем, не превышает ли новое количество складские остатки
                if (quantity > product.Stock)
                {
                    throw new InvalidOperationException($"Недостатньо товару на складі. Доступно: {product.Stock}, запитується: {quantity}");
                }

                cartItem.Quantity = quantity;
                cartItem.UpdatedAt = DateTime.UtcNow;
                _dataContext.SaveChanges();
            }
        }
    }

    public void ModifyCartItem(Guid userId, Guid productId, int increment)
    {
        var cartItem = GetCartItem(userId, productId);
        if (cartItem != null)
        {
            var newQuantity = cartItem.Quantity + increment;
            
            // Если новое количество меньше нуля, выбрасываем исключение
            if (newQuantity < 0)
            {
                throw new ArgumentException("Кількість товару не може бути від'ємною");
            }
            
            // Если новое количество равно нулю, удаляем товар
            if (newQuantity == 0)
            {
                RemoveFromCart(userId, productId);
                return;
            }
            
            // Получаем товар для проверки складских остатков
            var product = _dataContext.Products.FirstOrDefault(p => p.Id == productId && p.DeletedAt == null);
            if (product == null)
            {
                throw new ArgumentException("Товар не знайдено");
            }

            // Проверяем, не превышает ли новое количество складские остатки
            if (newQuantity > product.Stock)
            {
                throw new InvalidOperationException($"Недостатньо товару на складі. Доступно: {product.Stock}, запитується: {newQuantity}");
            }

            cartItem.Quantity = newQuantity;
            cartItem.UpdatedAt = DateTime.UtcNow;
            _dataContext.SaveChanges();
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

    // Методы для работы с заказами
    public void RepeatCart(Guid currentUserId, Guid sourceUserId, IEnumerable<CartItem> sourceCartItems)
    {
        // Проверка принадлежности повторяемого заказа тому же пользователю
        if (currentUserId != sourceUserId)
        {
            throw new UnauthorizedAccessException("Не можна повторювати замовлення іншого користувача");
        }

        // Проверка существования товаров и их доступности
        foreach (var item in sourceCartItems)
        {
            // Проверяем, что товар не был удален
            var product = _dataContext.Products.FirstOrDefault(p => p.Id == item.ProductId && p.DeletedAt == null);
            if (product == null)
            {
                throw new InvalidOperationException($"Товар '{item.Product?.Name ?? "Unknown"}' більше не існує або був видалений");
            }

            // Проверяем достаточность товара на складе
            if (product.Stock < item.Quantity)
            {
                throw new InvalidOperationException($"Недостатньо товару '{product.Name}' на складі. Доступно: {product.Stock}, потрібно: {item.Quantity}");
            }
        }

        // Очищаем текущую корзину пользователя
        ClearCart(currentUserId);

        // Добавляем товары из старого заказа в новую корзину
        foreach (var item in sourceCartItems)
        {
            var product = _dataContext.Products.FirstOrDefault(p => p.Id == item.ProductId && p.DeletedAt == null);
            if (product != null && product.Stock >= item.Quantity)
            {
                AddToCart(currentUserId, item.ProductId, item.Quantity);
            }
        }
    }

    public void CheckoutCart(Guid userId)
    {
        var cartItems = GetUserCartItems(userId).ToList();
        
        if (!cartItems.Any())
        {
            throw new InvalidOperationException("Корзина порожня");
        }

        // Проверяем доступность всех товаров перед оформлением
        foreach (var item in cartItems)
        {
            var product = _dataContext.Products.FirstOrDefault(p => p.Id == item.ProductId && p.DeletedAt == null);
            if (product == null)
            {
                throw new InvalidOperationException($"Товар '{item.Product?.Name ?? "Unknown"}' більше не існує");
            }

            if (product.Stock < item.Quantity)
            {
                throw new InvalidOperationException($"Недостатньо товару '{product.Name}' на складі. Доступно: {product.Stock}, потрібно: {item.Quantity}");
            }
        }

        // Создаем заказ
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            TotalAmount = cartItems.Sum(c => c.Quantity * c.Product.Price),
            ItemsCount = cartItems.Sum(c => c.Quantity),
            Status = "Completed",
            CompletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _dataContext.Orders.Add(order);

        // Создаем элементы заказа и уменьшаем складские остатки
        foreach (var item in cartItems)
        {
            var product = _dataContext.Products.FirstOrDefault(p => p.Id == item.ProductId && p.DeletedAt == null);
            if (product != null)
            {
                // Создаем элемент заказа с сохранением данных на момент заказа
                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    ProductName = product.Name,
                    ProductDescription = product.Description,
                    ProductPrice = product.Price,
                    Quantity = item.Quantity,
                    TotalPrice = item.Quantity * product.Price,
                    ProductImageUrl = product.ImageUrl,
                    ProductGroupName = product.Group?.Name ?? "Unknown",
                    CreatedAt = DateTime.UtcNow
                };

                _dataContext.OrderItems.Add(orderItem);

                // Уменьшаем складские остатки
                product.Stock -= item.Quantity;
                if (product.Stock < 0)
                {
                    product.Stock = 0; // Защита от отрицательных значений
                }
            }
        }

        // Очищаем корзину после успешного оформления
        ClearCart(userId);
        
        _dataContext.SaveChanges();
    }

    // Методы для работы с историей заказов
    public IEnumerable<Order> GetUserOrders(Guid userId)
    {
        return _dataContext.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.OrderDate)
            .AsEnumerable();
    }

    public Order? GetOrderById(Guid orderId, Guid userId)
    {
        return _dataContext.Orders
            .Where(o => o.Id == orderId && o.UserId == userId)
            .Include(o => o.OrderItems)
            .FirstOrDefault();
    }

    public void RepeatOrderWithQuantities(Guid userId, Guid orderId, List<Models.OrderItemQuantity>? customQuantities)
    {
        Console.WriteLine($"DataAccessor.RepeatOrderWithQuantities: userId = {userId}, orderId = {orderId}");
        
        var order = GetOrderById(orderId, userId);
        Console.WriteLine($"DataAccessor.RepeatOrderWithQuantities: order found = {order != null}");
        
        if (order == null)
        {
            Console.WriteLine($"DataAccessor.RepeatOrderWithQuantities: order not found for userId={userId}, orderId={orderId}");
            throw new InvalidOperationException("Замовлення не знайдено або не належить вам");
        }

        // Очищаем текущую корзину
        ClearCart(userId);

        // Добавляем товары из заказа в корзину с пользовательскими количествами
        foreach (var orderItem in order.OrderItems)
        {
            // Находим пользовательское количество для этого товара
            var customQuantity = customQuantities?.FirstOrDefault(i => i.ProductId == orderItem.ProductId.ToString());
            var quantityToAdd = customQuantity?.Quantity ?? orderItem.Quantity;
            
            Console.WriteLine($"Adding product {orderItem.ProductId} with quantity {quantityToAdd}");
            
            // Проверяем, что товар все еще существует
            var product = _dataContext.Products.FirstOrDefault(p => p.Id == orderItem.ProductId && p.DeletedAt == null);
            if (product != null)
            {
                // Проверяем доступность товара
                if (product.Stock >= quantityToAdd)
                {
                    AddToCart(userId, orderItem.ProductId, quantityToAdd);
                }
                else
                {
                    // Если товара недостаточно, добавляем доступное количество
                    if (product.Stock > 0)
                    {
                        AddToCart(userId, orderItem.ProductId, product.Stock);
                    }
                }
            }
        }
        
        Console.WriteLine("DataAccessor.RepeatOrderWithQuantities: completed successfully");
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
