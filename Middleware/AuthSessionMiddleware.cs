using ASP_421.Data.Entities;
using System.Security.Claims;
using System.Text.Json;

namespace ASP_421.Middleware
{
    public class AuthSessionMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthSessionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Query.ContainsKey("logout"))
            {
                context.Session.Remove("SignIn");
                context.Response.Redirect(context.Request.Path);
                return;
            }

            if (context.Session.Keys.Contains("SignIn"))
            {
                try
                {
                    UserAccess userAccess =
                        JsonSerializer.Deserialize<UserAccess>(
                            context.Session.GetString("SignIn")!)!;

                    // Проверяем, что пользователь не удален
                    if (userAccess.User.DeletedAt != null)
                    {
                        // Если пользователь удален, очищаем сессию
                        context.Session.Remove("SignIn");
                        return;
                    }

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, userAccess.User.Name),
                        new Claim(ClaimTypes.Email, userAccess.User.Email),
                        new Claim("Id", userAccess.User.Id.ToString()),
                        new Claim(ClaimTypes.NameIdentifier, userAccess.Login),
                        new Claim(ClaimTypes.Role, userAccess.RoleId)
                    };

                    // Добавляем сведения о дате рождения, если она есть
                    if (userAccess.User.Birthdate.HasValue)
                    {
                        claims.Add(new Claim(ClaimTypes.DateOfBirth, userAccess.User.Birthdate.Value.ToString("yyyy-MM-dd")));
                    }

                    context.User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            claims,
                            nameof(AuthSessionMiddleware)
                        )
                    );
                }
                catch (Exception)
                {
                    // Если произошла ошибка при десериализации, очищаем сессию
                    context.Session.Remove("SignIn");
                }
            }
            await _next(context);
        }
    }

    public static class AuthSessionMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthSession(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthSessionMiddleware>();
        }
    }

}
