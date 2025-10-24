using ASP_421.Models;
using ASP_421.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ASP_421.Controllers
{
    public class OrdersController : BaseController
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public async Task<IActionResult> History()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToSignUp();
            }

            var orders = await _orderService.GetUserOrdersAsync(userId.Value);
            
            ViewData["Orders"] = orders;
            ViewData["Title"] = "История заказов";
            
            return View();
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToSignUp();
            }

            var order = await _orderService.GetOrderByIdAsync(id, userId.Value);
            if (order == null)
            {
                return NotFound();
            }

            ViewData["Order"] = order;
            ViewData["Title"] = $"Заказ #{order.Id.ToString().Substring(0, 8)}";
            
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RepeatOrder([FromBody] RepeatOrderRequest request)
        {
            Console.WriteLine($"RepeatOrder called with orderId = '{request.OrderId}'");
            Console.WriteLine($"Items count: {request.Items?.Count ?? 0}");
            
            if (!Guid.TryParse(request.OrderId, out var orderId))
            {
                Console.WriteLine($"RepeatOrder: failed to parse orderId from '{request.OrderId}'");
                return Json(new { success = false, message = "Невірний формат ID замовлення" });
            }
            
            Console.WriteLine($"RepeatOrder: parsed orderId = {orderId}");
            
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                Console.WriteLine("RepeatOrder: userId is null, user not authenticated");
                return Json(new { success = false, message = "Користувач не авторизований" });
            }

            Console.WriteLine($"RepeatOrder: userId = {userId}, orderId = {orderId}");

            try
            {
                var response = await _orderService.RepeatOrderWithQuantitiesAndCheckAsync(userId.Value, orderId, request.Items);
                Console.WriteLine("RepeatOrder: success");
                return Json(response);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"RepeatOrder: InvalidOperationException - {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RepeatOrder: Exception - {ex.Message}");
                return Json(new { success = false, message = "Сталась помилка, повторіть дію пізніше" });
            }
        }
    }
}
