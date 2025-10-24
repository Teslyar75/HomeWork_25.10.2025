using ASP_421.Models.Shop.Api;
using ASP_421.Services.Interfaces;
using ASP_421.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ASP_421.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController(ICartService cartService, IOrderService orderService) : ControllerBase
    {
        private readonly ICartService _cartService = cartService;
        private readonly IOrderService _orderService = orderService;

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("Id")?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return null;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = GetCurrentUserId();
            
            // Добавляем логирование для отладки
            Console.WriteLine($"GetCart - User claims: {string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
            Console.WriteLine($"GetCart - Extracted userId: {userId}");
            
            if (userId == null)
            {
                return Ok(new CartApiResponse
                {
                    Status = "Error",
                    ErrorMessage = "Користувач не авторизований"
                });
            }

            try
            {
                var cartItems = await _cartService.GetUserCartItemsAsync(userId.Value);
                var cartSummary = new CartSummaryApiModel
                {
                    ItemsCount = await _cartService.GetCartItemsCountAsync(userId.Value),
                    TotalAmount = await _cartService.GetCartTotalAsync(userId.Value),
                    Items = cartItems.Select(item => new CartItemApiModel
                    {
                        Id = item.Id,
                        ProductId = item.ProductId,
                        ProductName = item.Product?.Name ?? "Unknown",
                        ProductDescription = item.Product?.Description ?? "",
                        ProductImageUrl = item.Product?.ImageUrl ?? "",
                        ProductPrice = item.Product?.Price ?? 0,
                        Quantity = item.Quantity,
                        TotalPrice = item.Quantity * (item.Product?.Price ?? 0),
                        AddedAt = item.AddedAt
                    }).ToList()
                };

                return Ok(new CartApiResponse
                {
                    Status = "Ok",
                    Data = cartSummary
                });
            }
            catch (Exception ex)
            {
                return Ok(new CartApiResponse
                {
                    Status = "Error",
                    ErrorMessage = $"Помилка отримання корзини: {ex.Message}"
                });
            }
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            var userId = GetCurrentUserId();
            
            // Добавляем логирование для отладки
            Console.WriteLine($"AddToCart - User claims: {string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
            Console.WriteLine($"AddToCart - Extracted userId: {userId}");
            Console.WriteLine($"AddToCart - ProductId: {request.ProductId}, Quantity: {request.Quantity}");
            
            if (userId == null)
            {
                return Ok(new CartApiResponse
                {
                    Status = "Error",
                    ErrorMessage = "Користувач не авторизований"
                });
            }

            try
            {
                await _cartService.AddToCartAsync(userId.Value, request.ProductId, request.Quantity);
                
                return Ok(new CartApiResponse
                {
                    Status = "Ok",
                    Message = "Товар успішно додано до корзини"
                });
            }
            catch (ArgumentException ex)
            {
                return Ok(new CartApiResponse
                {
                    Status = "Error",
                    ErrorMessage = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Ok(new CartApiResponse
                {
                    Status = "Error",
                    ErrorMessage = ex.Message
                });
            }
            catch (Exception ex)
            {
                return Ok(new CartApiResponse
                {
                    Status = "Error",
                    ErrorMessage = "Сталась помилка, повторіть дію пізніше"
                });
            }
        }

        [HttpPut("modify")]
        public async Task<IActionResult> ModifyCartItem([FromBody] ModifyCartItemRequest request)
        {
            var userId = GetCurrentUserId();
            
            if (userId == null)
            {
                return Ok(new CartApiResponse
                {
                    Status = "Error",
                    ErrorMessage = "Користувач не авторизований"
                });
            }

            try
            {
                await _cartService.ModifyCartItemAsync(userId.Value, request.ProductId, request.Increment);
                
                return Ok(new CartApiResponse
                {
                    Status = "Ok",
                    Message = "Кількість товару успішно оновлено"
                });
            }
            catch (ArgumentException ex)
            {
                return Ok(new CartApiResponse
                {
                    Status = "Error",
                    ErrorMessage = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Ok(new CartApiResponse
                {
                    Status = "Error",
                    ErrorMessage = ex.Message
                });
            }
            catch (Exception ex)
            {
                return Ok(new CartApiResponse
                {
                    Status = "Error",
                    ErrorMessage = "Сталась помилка, повторіть дію пізніше"
                });
            }
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> CheckoutCart()
        {
            var userId = GetCurrentUserId();
            
            if (userId == null)
            {
                return Ok(new CartApiResponse
                {
                    Status = "Error",
                    ErrorMessage = "Користувач не авторизований"
                });
            }

            try
            {
                var order = await _orderService.CreateOrderFromCartAsync(userId.Value);
                
                return Ok(new CartApiResponse
                {
                    Status = "Ok",
                    Message = "Замовлення успішно оформлено",
                    Data = new { OrderId = order.Id }
                });
            }
            catch (InvalidOperationException ex)
            {
                return Ok(new CartApiResponse
                {
                    Status = "Error",
                    ErrorMessage = ex.Message
                });
            }
            catch (Exception ex)
            {
                return Ok(new CartApiResponse
                {
                    Status = "Error",
                    ErrorMessage = "Сталась помилка, повторіть дію пізніше"
                });
            }
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetCartItemsCount()
        {
            var userId = GetCurrentUserId();
            
            if (userId == null)
            {
                return Ok(new CartApiResponse
                {
                    Status = "Ok",
                    Data = new { Count = 0 }
                });
            }

            try
            {
                var count = await _cartService.GetCartItemsCountAsync(userId.Value);
                
                return Ok(new CartApiResponse
                {
                    Status = "Ok",
                    Data = new { Count = count }
                });
            }
            catch (Exception ex)
            {
                return Ok(new CartApiResponse
                {
                    Status = "Error",
                    ErrorMessage = "Сталась помилка, повторіть дію пізніше"
                });
            }
        }

        [HttpGet("last-order")]
        public async Task<IActionResult> GetLastOrder()
        {
            var userId = GetCurrentUserId();
            
            if (userId == null)
            {
                return Ok(new CartApiResponse
                {
                    Status = "Error",
                    ErrorMessage = "Користувач не авторизований"
                });
            }

            try
            {
                var orders = await _orderService.GetUserOrdersAsync(userId.Value);
                var lastOrder = orders.FirstOrDefault();
                
                if (lastOrder == null)
                {
                    return Ok(new CartApiResponse
                    {
                        Status = "Error",
                        ErrorMessage = "Немає замовлень для повторення"
                    });
                }

                return Ok(new CartApiResponse
                {
                    Status = "Ok",
                    Data = new { OrderId = lastOrder.Id }
                });
            }
            catch (Exception ex)
            {
                return Ok(new CartApiResponse
                {
                    Status = "Error",
                    ErrorMessage = "Сталась помилка, повторіть дію пізніше"
                });
            }
        }
    }
}
