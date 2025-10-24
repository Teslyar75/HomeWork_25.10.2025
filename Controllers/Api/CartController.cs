using ASP_421.Data;
using ASP_421.Models.Shop.Api;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ASP_421.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController(DataAccessor dataAccessor) : ControllerBase
    {
        private readonly DataAccessor _dataAccessor = dataAccessor;

        private Guid? GetCurrentUserId()
        {
            // Сначала пробуем найти claim с типом "Id" (как в UserController)
            var userIdClaim = User.FindFirst("Id")?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            
            // Если не найден, пробуем ClaimTypes.NameIdentifier
            userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out userId) ? userId : null;
        }

        [HttpGet]
        public IActionResult GetCart()
        {
            var userId = GetCurrentUserId();
            
            // Логируем информацию о пользователе для диагностики
            Console.WriteLine($"Cart API GET - User claims: {string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
            Console.WriteLine($"Cart API GET - Extracted userId: {userId}");
            
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
                var cartItems = _dataAccessor.GetUserCartItems(userId.Value);
                var cartSummary = new CartSummaryApiModel
                {
                    ItemsCount = _dataAccessor.GetCartItemsCount(userId.Value),
                    TotalAmount = _dataAccessor.GetCartTotal(userId.Value),
                    Items = cartItems.Select(item => new CartItemApiModel
                    {
                        Id = item.Id,
                        ProductId = item.ProductId,
                        ProductName = item.Product.Name,
                        ProductDescription = item.Product.Description,
                        ProductImageUrl = item.Product.ImageUrl,
                        ProductPrice = item.Product.Price,
                        Quantity = item.Quantity,
                        TotalPrice = item.Quantity * item.Product.Price,
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
        public IActionResult AddToCart([FromBody] AddToCartRequest request)
        {
            var userId = GetCurrentUserId();
            
            // Логируем информацию о пользователе для диагностики
            Console.WriteLine($"Cart API - User claims: {string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
            Console.WriteLine($"Cart API - Extracted userId: {userId}");
            
            if (userId == null)
            {
                return Ok(new CartApiResponse
                {
                    Status = "Error",
                    ErrorMessage = "Користувач не авторизований"
                });
            }

            if (request == null)
            {
                return Ok(new CartApiResponse
                {
                    Status = "Error",
                    ErrorMessage = "Некоректні дані запиту"
                });
            }

            if (request.ProductId == Guid.Empty)
            {
                return Ok(new CartApiResponse
                {
                    Status = "Error",
                    ErrorMessage = "Некоректний ID товару"
                });
            }

            if (request.Quantity <= 0)
            {
                return Ok(new CartApiResponse
                {
                    Status = "Error",
                    ErrorMessage = "Кількість товару повинна бути більше нуля"
                });
            }

            try
            {
                _dataAccessor.AddToCart(userId.Value, request.ProductId, request.Quantity);
                
                return Ok(new CartApiResponse
                {
                    Status = "Ok",
                    Data = new { Message = "Товар додано до корзини" }
                });
            }
            catch (Exception ex)
            {
                return Ok(new CartApiResponse
                {
                    Status = "Error",
                    ErrorMessage = $"Помилка додавання до корзини: {ex.Message}"
                });
            }
        }

        [HttpPut("update")]
        public IActionResult UpdateCartItem([FromBody] UpdateCartItemRequest request)
        {
            var userId = GetCurrentUserId();
            
            // Логируем информацию о запросе для диагностики
            Console.WriteLine($"Cart API UPDATE - User claims: {string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
            Console.WriteLine($"Cart API UPDATE - Extracted userId: {userId}");
            Console.WriteLine($"Cart API UPDATE - Request is null: {request == null}");
            
            if (request != null)
            {
                Console.WriteLine($"Cart API UPDATE - Request: ProductId={request.ProductId}, Quantity={request.Quantity}");
                Console.WriteLine($"Cart API UPDATE - ProductId is empty: {request.ProductId == Guid.Empty}");
            }
            else
            {
                Console.WriteLine("Cart API UPDATE - Request is NULL!");
            }
            
            if (userId == null)
            {
                return Ok(new CartApiResponse
                {
                    Status = "Error",
                    ErrorMessage = "Користувач не авторизований"
                });
            }

            if (request == null)
            {
                return Ok(new CartApiResponse
                {
                    Status = "Error",
                    ErrorMessage = "Некоректні дані запиту - request is null"
                });
            }

            if (request.ProductId == Guid.Empty)
            {
                return Ok(new CartApiResponse
                {
                    Status = "Error",
                    ErrorMessage = "Некоректний ID товару"
                });
            }

            if (request.Quantity <= 0)
            {
                return Ok(new CartApiResponse
                {
                    Status = "Error",
                    ErrorMessage = "Кількість товару повинна бути більше нуля"
                });
            }

            try
            {
                _dataAccessor.UpdateCartItemQuantity(userId.Value, request.ProductId, request.Quantity);
                
                return Ok(new CartApiResponse
                {
                    Status = "Ok",
                    Data = new { Message = "Кількість товару оновлено" }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cart API UPDATE - Exception: {ex.Message}");
                return Ok(new CartApiResponse
                {
                    Status = "Error",
                    ErrorMessage = $"Помилка оновлення корзини: {ex.Message}"
                });
            }
        }

        [HttpDelete("{productId}")]
        public IActionResult RemoveFromCart(Guid productId)
        {
            var userId = GetCurrentUserId();
            
            // Логируем информацию о запросе для диагностики
            Console.WriteLine($"Cart API DELETE - User claims: {string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
            Console.WriteLine($"Cart API DELETE - Extracted userId: {userId}");
            Console.WriteLine($"Cart API DELETE - ProductId: {productId}");
            
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
                _dataAccessor.RemoveFromCart(userId.Value, productId);
                
                return Ok(new CartApiResponse
                {
                    Status = "Ok",
                    Data = new { Message = "Товар видалено з корзини" }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cart API DELETE - Exception: {ex.Message}");
                return Ok(new CartApiResponse
                {
                    Status = "Error",
                    ErrorMessage = $"Помилка видалення з корзини: {ex.Message}"
                });
            }
        }

        [HttpDelete("clear")]
        public IActionResult ClearCart()
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
                _dataAccessor.ClearCart(userId.Value);
                
                return Ok(new CartApiResponse
                {
                    Status = "Ok",
                    Data = new { Message = "Корзина очищена" }
                });
            }
            catch (Exception ex)
            {
                return Ok(new CartApiResponse
                {
                    Status = "Error",
                    ErrorMessage = $"Помилка очищення корзини: {ex.Message}"
                });
            }
        }

        [HttpGet("count")]
        public IActionResult GetCartItemsCount()
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
                var count = _dataAccessor.GetCartItemsCount(userId.Value);
                
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
                    ErrorMessage = $"Помилка отримання кількості товарів: {ex.Message}"
                });
            }
        }
    }
}
