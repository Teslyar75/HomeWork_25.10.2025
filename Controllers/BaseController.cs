using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ASP_421.Controllers
{
    public abstract class BaseController : Controller
    {
        protected Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("Id")?.Value;
            
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            
            return null;
        }

        protected bool IsAuthenticated => User.Identity?.IsAuthenticated ?? false;

        protected string? GetCurrentUserLogin()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        protected string? GetCurrentUserName()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value;
        }

        protected IActionResult RedirectToSignUp()
        {
            return RedirectToAction("SignUp", "User");
        }

        protected IActionResult RedirectToLogin()
        {
            return RedirectToAction("Login", "User");
        }
    }
}
