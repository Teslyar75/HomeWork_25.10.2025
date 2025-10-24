using ASP_421.Data;
using ASP_421.Data.Entities;
using ASP_421.Models;
using ASP_421.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ASP_421.Services
{
    public class OrderService : IOrderService
    {
        private readonly DataContext _dataContext;
        private readonly ICartService _cartService;

        public OrderService(DataContext dataContext, ICartService cartService)
        {
            _dataContext = dataContext;
            _cartService = cartService;
        }

        public async Task<IEnumerable<Order>> GetUserOrdersAsync(Guid userId)
        {
            return await _dataContext.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Order?> GetOrderByIdAsync(Guid orderId, Guid userId)
        {
            return await _dataContext.Orders
                .Where(o => o.Id == orderId && o.UserId == userId)
                .Include(o => o.OrderItems)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task<Order> CreateOrderFromCartAsync(Guid userId)
        {
            var cartItems = await _cartService.GetUserCartItemsAsync(userId);
            var cartItemsList = cartItems.ToList();
            
            if (!cartItemsList.Any())
            {
                throw new InvalidOperationException("Корзина порожня");
            }

            // Получаем все товары одним запросом для избежания N+1
            var productIds = cartItemsList.Select(item => item.ProductId).ToList();
            var products = await _dataContext.Products
                .Where(p => productIds.Contains(p.Id) && p.DeletedAt == null)
                .ToDictionaryAsync(p => p.Id, p => p);

            // Проверяем все товары перед созданием заказа
            foreach (var item in cartItemsList)
            {
                if (!products.TryGetValue(item.ProductId, out var product))
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
                TotalAmount = cartItemsList.Sum(c => c.Quantity * c.Product.Price),
                ItemsCount = cartItemsList.Sum(c => c.Quantity),
                Status = "Completed",
                CompletedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _dataContext.Orders.Add(order);

            // Создаем элементы заказа и уменьшаем складские остатки
            foreach (var item in cartItemsList)
            {
                if (products.TryGetValue(item.ProductId, out var product))
                {
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
            await _cartService.ClearCartAsync(userId);
            
            await _dataContext.SaveChangesAsync();
            return order;
        }

        public async Task RepeatOrderAsync(Guid userId, Guid orderId)
        {
            var order = await GetOrderByIdAsync(orderId, userId);
            if (order == null)
            {
                throw new InvalidOperationException("Замовлення не знайдено або не належить вам");
            }

            // Очищаем текущую корзину
            await _cartService.ClearCartAsync(userId);

            // Добавляем товары из заказа в корзину
            foreach (var orderItem in order.OrderItems)
            {
                // Проверяем, что товар все еще существует
                var product = await _dataContext.Products
                    .FirstOrDefaultAsync(p => p.Id == orderItem.ProductId && p.DeletedAt == null);
                
                if (product != null)
                {
                    // Проверяем доступность товара
                    if (product.Stock >= orderItem.Quantity)
                    {
                        await _cartService.AddToCartAsync(userId, orderItem.ProductId, orderItem.Quantity);
                    }
                    else
                    {
                        // Если товара недостаточно, добавляем доступное количество
                        if (product.Stock > 0)
                        {
                            await _cartService.AddToCartAsync(userId, orderItem.ProductId, product.Stock);
                        }
                    }
                }
            }
        }

        public async Task RepeatOrderWithQuantitiesAsync(Guid userId, Guid orderId, List<OrderItemQuantity>? customQuantities)
        {
            Console.WriteLine($"OrderService.RepeatOrderWithQuantities: userId = {userId}, orderId = {orderId}");
            
            var order = await GetOrderByIdAsync(orderId, userId);
            Console.WriteLine($"OrderService.RepeatOrderWithQuantities: order found = {order != null}");
            
            if (order == null)
            {
                Console.WriteLine($"OrderService.RepeatOrderWithQuantities: order not found for userId={userId}, orderId={orderId}");
                throw new InvalidOperationException("Замовлення не знайдено або не належить вам");
            }

            // Очищаем текущую корзину
            await _cartService.ClearCartAsync(userId);

            // Получаем все товары одним запросом для избежания N+1
            var productIds = order.OrderItems.Select(oi => oi.ProductId).ToList();
            var products = await _dataContext.Products
                .Where(p => productIds.Contains(p.Id) && p.DeletedAt == null)
                .ToDictionaryAsync(p => p.Id, p => p);

            // Добавляем товары из заказа в корзину с пользовательскими количествами
            foreach (var orderItem in order.OrderItems)
            {
                // Находим пользовательское количество для этого товара
                var customQuantity = customQuantities?.FirstOrDefault(i => i.ProductId == orderItem.ProductId.ToString());
                var quantityToAdd = customQuantity?.Quantity ?? orderItem.Quantity;
                
                Console.WriteLine($"Adding product {orderItem.ProductId} with quantity {quantityToAdd}");
                
                // Проверяем, что товар все еще существует
                if (products.TryGetValue(orderItem.ProductId, out var product))
                {
                    // Проверяем доступность товара
                    if (product.Stock >= quantityToAdd)
                    {
                        await _cartService.AddToCartAsync(userId, orderItem.ProductId, quantityToAdd);
                    }
                    else
                    {
                        // Если товара недостаточно, добавляем доступное количество
                        if (product.Stock > 0)
                        {
                            await _cartService.AddToCartAsync(userId, orderItem.ProductId, product.Stock);
                        }
                    }
                }
            }
            
            Console.WriteLine("OrderService.RepeatOrderWithQuantities: completed successfully");
        }

        public async Task<RepeatOrderResponse> RepeatOrderWithQuantitiesAndCheckAsync(Guid userId, Guid orderId, List<OrderItemQuantity>? customQuantities)
        {
            Console.WriteLine($"OrderService.RepeatOrderWithQuantitiesAndCheck: userId = {userId}, orderId = {orderId}");
            
            var order = await GetOrderByIdAsync(orderId, userId);
            Console.WriteLine($"OrderService.RepeatOrderWithQuantitiesAndCheck: order found = {order != null}");
            
            if (order == null)
            {
                Console.WriteLine($"OrderService.RepeatOrderWithQuantitiesAndCheck: order not found for userId={userId}, orderId={orderId}");
                throw new InvalidOperationException("Замовлення не знайдено або не належить вам");
            }

            // Очищаем текущую корзину
            await _cartService.ClearCartAsync(userId);

            // Получаем все товары одним запросом для избежания N+1
            var productIds = order.OrderItems.Select(oi => oi.ProductId).ToList();
            var products = await _dataContext.Products
                .Where(p => productIds.Contains(p.Id) && p.DeletedAt == null)
                .ToDictionaryAsync(p => p.Id, p => p);

            var unavailableProducts = new List<UnavailableProduct>();
            var addedProductsCount = 0;

            // Добавляем товары из заказа в корзину с пользовательскими количествами
            foreach (var orderItem in order.OrderItems)
            {
                // Находим пользовательское количество для этого товара
                var customQuantity = customQuantities?.FirstOrDefault(i => i.ProductId == orderItem.ProductId.ToString());
                var quantityToAdd = customQuantity?.Quantity ?? orderItem.Quantity;
                
                Console.WriteLine($"Adding product {orderItem.ProductId} with quantity {quantityToAdd}");
                
                // Проверяем, что товар все еще существует
                if (products.TryGetValue(orderItem.ProductId, out var product))
                {
                    // Проверяем доступность товара
                    if (product.Stock >= quantityToAdd)
                    {
                        await _cartService.AddToCartAsync(userId, orderItem.ProductId, quantityToAdd);
                        addedProductsCount++;
                    }
                    else
                    {
                        // Если товара недостаточно, добавляем доступное количество
                        if (product.Stock > 0)
                        {
                            await _cartService.AddToCartAsync(userId, orderItem.ProductId, product.Stock);
                            addedProductsCount++;
                            
                            unavailableProducts.Add(new UnavailableProduct
                            {
                                ProductName = product.Name,
                                RequestedQuantity = quantityToAdd,
                                AvailableQuantity = product.Stock,
                                Message = $"Товар '{product.Name}' закінчився. Додано {product.Stock} шт. замість {quantityToAdd} шт."
                            });
                        }
                        else
                        {
                            unavailableProducts.Add(new UnavailableProduct
                            {
                                ProductName = product.Name,
                                RequestedQuantity = quantityToAdd,
                                AvailableQuantity = 0,
                                Message = $"Товар '{product.Name}' закінчився і буде виключений з замовлення."
                            });
                        }
                    }
                }
                else
                {
                    unavailableProducts.Add(new UnavailableProduct
                    {
                        ProductName = orderItem.ProductName,
                        RequestedQuantity = quantityToAdd,
                        AvailableQuantity = 0,
                        Message = $"Товар '{orderItem.ProductName}' більше не доступний і буде виключений з замовлення."
                    });
                }
            }
            
            Console.WriteLine($"OrderService.RepeatOrderWithQuantitiesAndCheck: completed. Added {addedProductsCount} products, {unavailableProducts.Count} unavailable");

            var response = new RepeatOrderResponse
            {
                Success = true,
                Message = unavailableProducts.Count == 0 
                    ? "Замовлення повторено. Всі товари додано до корзини." 
                    : $"Замовлення повторено. Додано {addedProductsCount} товарів. {unavailableProducts.Count} товарів недоступні.",
                UnavailableProducts = unavailableProducts.Count > 0 ? unavailableProducts : null
            };

            return response;
        }

        public async Task<bool> CanRepeatOrderAsync(Guid userId, Guid orderId)
        {
            var order = await GetOrderByIdAsync(orderId, userId);
            return order != null;
        }
    }
}
