using ASP_421.Data.Entities;
using ASP_421.Models;

namespace ASP_421.Services.Interfaces
{
    public interface IOrderService
    {
        Task<IEnumerable<Order>> GetUserOrdersAsync(Guid userId);
        Task<Order?> GetOrderByIdAsync(Guid orderId, Guid userId);
        Task<Order> CreateOrderFromCartAsync(Guid userId);
        Task RepeatOrderAsync(Guid userId, Guid orderId);
        Task RepeatOrderWithQuantitiesAsync(Guid userId, Guid orderId, List<OrderItemQuantity>? customQuantities);
        Task<RepeatOrderResponse> RepeatOrderWithQuantitiesAndCheckAsync(Guid userId, Guid orderId, List<OrderItemQuantity>? customQuantities);
        Task<bool> CanRepeatOrderAsync(Guid userId, Guid orderId);
    }
}
