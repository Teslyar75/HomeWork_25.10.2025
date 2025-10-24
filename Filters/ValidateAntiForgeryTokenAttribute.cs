using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ASP_421.Filters
{
    public class ValidateAntiForgeryTokenAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Проверяем только для POST, PUT, DELETE запросов
            if (context.HttpContext.Request.Method != "GET")
            {
                var token = context.HttpContext.Request.Headers["X-CSRF-Token"].FirstOrDefault();
                var cookieToken = context.HttpContext.Request.Cookies["CSRF-Token"];
                
                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(cookieToken) || token != cookieToken)
                {
                    context.Result = new BadRequestObjectResult("CSRF token validation failed");
                    return;
                }
            }
            
            base.OnActionExecuting(context);
        }
    }
}
