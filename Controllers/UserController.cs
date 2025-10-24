using ASP_421.Data;
using ASP_421.Models.User;
using ASP_421.Services.Kdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace ASP_421.Controllers
{
    public class UserController(
        DataContext dataContext, 
        IKdfService kdfService) : Controller
    {
        private readonly DataContext _dataContext = dataContext;
        private readonly IKdfService _kdfService = kdfService;

        const String RegisterKey = "RegisterFormModel";

        public IActionResult Profile(String id)
        {
            UserProfileViewModel viewModel = new();

            viewModel.User = _dataContext
                .UserAccesses
                .Include(ua => ua.User)
                .AsNoTracking()
                .FirstOrDefault(ua => ua.Login == id && ua.User.DeletedAt == null)
                ?.User;

            var authUserId = HttpContext
                .User
                .Claims
                .FirstOrDefault(c => c.Type == "Id")
                ?.Value;
            viewModel.IsPersonal = authUserId != null
                && authUserId == viewModel.User?.Id.ToString();

            // Если это личный кабинет, загружаем данные корзины и статистику
            if (viewModel.IsPersonal && viewModel.User != null)
            {
                var userId = viewModel.User.Id;
                
                // Данные корзины
                viewModel.CartItems = _dataContext.CartItems
                    .Where(c => c.UserId == userId)
                    .Include(c => c.Product)
                    .ThenInclude(p => p.Group)
                    .AsNoTracking()
                    .ToList();
                
                viewModel.CartItemsCount = viewModel.CartItems.Count();
                viewModel.CartTotalAmount = viewModel.CartItems.Sum(c => c.Quantity * c.Product.Price);
                
                // Статистика пользователя (пока что заглушки, так как нет таблицы заказов)
                viewModel.TotalOrdersCount = 0; // Будет реализовано позже
                viewModel.TotalSpentAmount = 0m; // Будет реализовано позже
                viewModel.LastOrderDate = null; // Будет реализовано позже
                viewModel.LastLoginDate = viewModel.User.RegisteredAt; // Используем дату регистрации
                
                // Активность
                viewModel.DaysSinceRegistration = (DateTime.Now - viewModel.User.RegisteredAt).Days;
                viewModel.IsActiveUser = viewModel.DaysSinceRegistration <= 30; // Активен если зарегистрирован менее 30 дней назад
            }

            return View(viewModel);
        }

        [HttpPatch]
        public JsonResult Update([FromBody] JsonElement json)
        {
            if (json.GetPropertyCount() == 0)
            {
                return Json(new
                {
                    Status = 400,
                    Data = "Missing data to update"
                });
            }
            if (!(HttpContext.User.Identity?.IsAuthenticated ?? false))
            {
                return Json(new
                {
                    Status = 401,
                    Data = "Unauthorized"
                });
            }
            String id = HttpContext.User.Claims.First(c => c.Type == "Id").Value;

            Data.Entities.User user = _dataContext
                .Users
                .Find(Guid.Parse(id))!;

            // Валидация и обновление имени
            if (json.TryGetProperty("Name", out JsonElement name))
            {
                string newName = name.GetString()!;
                var nameValidation = ValidateName(newName);
                if (nameValidation.IsValid)
                {
                    user.Name = newName;
                }
                else
                {
                    return Json(new
                    {
                        Status = 400,
                        Data = nameValidation.ErrorMessage
                    });
                }
            }

            // Валидация и обновление email
            if (json.TryGetProperty("Email", out JsonElement email))
            {
                string newEmail = email.GetString()!;
                var emailValidation = ValidateEmail(newEmail, user.Id);
                if (emailValidation.IsValid)
                {
                    user.Email = newEmail;
                }
                else
                {
                    return Json(new
                    {
                        Status = 400,
                        Data = emailValidation.ErrorMessage
                    });
                }
            }

            _dataContext.SaveChanges();

            // Обновляем данные в сессии
            if (HttpContext.Session.Keys.Contains("SignIn"))
            {
                var userAccess = _dataContext.UserAccesses
                    .Include(ua => ua.User)
                    .FirstOrDefault(ua => ua.UserId == user.Id);
                
                if (userAccess != null)
                {
                    HttpContext.Session.SetString("SignIn", JsonSerializer.Serialize(userAccess));
                }
            }

            return  Json(new
            {
                Status = 200,
                Data = "Ok"
            });
        }

        [HttpDelete]
        public JsonResult Delete()
        {
            // перевірити чи користувач авторизованний
            if (!(HttpContext.User.Identity?.IsAuthenticated ?? false))
            {
                return Json(new
                {
                    Status = 401,
                    Data = "Unauthorized"
                });
            }

            // визначитит його ID  та відшукати об'єкт БД
            String id = HttpContext.User.Claims.First(c => c.Type == "Id").Value;
            Data.Entities.User user = _dataContext.Users.Find(Guid.Parse(id))!;

            if (user == null)
            {
                return Json(new
                {
                    Status = 404,
                    Data = "User not found"
                });
            }

            //видалити персональні дані (Name? Bithdate) - якщо можливо
            // то встановлюємо NULL , якщо ні -порожній об'єкт
            user.Name = "Deleted User";
            user.Email = "deleted@example.com";
            user.Birthdate = null;
            
            // встановити дату видалення 
            user.DeletedAt = DateTime.UtcNow;
            
            _dataContext.SaveChanges();

            // Очищаем сессию после удаления
            HttpContext.Session.Remove("SignIn");

            return Json(new
            {
                Status = 200,
                Data = "User deleted successfully"
            });
        }
        public JsonResult SignIn()
        {
            // Authorization: Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ==

            String header = // Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ==
                HttpContext.Request.Headers.Authorization.ToString();
            if (String.IsNullOrEmpty(header))
            {
                return Json(new { 
                    Status = 401, 
                    Data = "Missing Authorization header" });
            }
            String scheme = "Basic ";
            if(!header.StartsWith(scheme))
            {
                return Json(new
                {
                    Status = 401,
                    Data = "Invalid scheme. Required: " + scheme 
                });
            }
            String credentials =   // QWxhZGRpbjpvcGVuIHNlc2FtZQ==
                header[scheme.Length..];

            // Проверяем, что credentials не пусты
            if (String.IsNullOrEmpty(credentials))
            {
                return Json(new
                {
                    Status = 401,
                    Data = "Empty credentials"
                });
            }

            String userPass;
            try
            {
                // Декодирование credentials по Base64 должно состояться успешно
                userPass = Encoding.UTF8.GetString(
                    Convert.FromBase64String(credentials));
            }
            catch (FormatException)
            {
                return Json(new
                {
                    Status = 401,
                    Data = "Invalid Base64 format"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Status = 401,
                    Data = "Decoding error: " + ex.Message
                });
            }

            // Декодированные данные должны содержать ":" и делиться на две части
            if (!userPass.Contains(':'))
            {
                return Json(new
                {
                    Status = 401,
                    Data = "Invalid credentials format - missing separator"
                });
            }

            String[] parts = userPass.Split(':', 2);
            
            // После разделения каждая из частей не должна быть пуста
            if (parts.Length != 2 || String.IsNullOrEmpty(parts[0]) || String.IsNullOrEmpty(parts[1]))
            {
                return Json(new
                {
                    Status = 401,
                    Data = "Invalid credentials - empty login or password"
                });
            }

            String login = parts[0];     // Aladdin
            String password = parts[1];  // open sesame

            var userAccess = _dataContext
                .UserAccesses
                .AsNoTracking()           // не моніторити зміни -- тільки читання
                .Include(ua => ua.User)   // заповнення навігаційної властивості
                .FirstOrDefault(ua => ua.Login == login && ua.User.DeletedAt == null);

            if (userAccess == null)
            {
                return Json(new
                {
                    Status = 401,
                    Data = "Виникла помилка, спробуйте ще раз."
                });
            }
            if(_kdfService.Dk(password, userAccess.Salt) != userAccess.Dk)
            {
                return Json(new
                {
                    Status = 401,
                    Data = "Credentials rejected"
                });
            }
            // користувач пройшов автентифікацію, зберігаємо у сесії
            HttpContext.Session.SetString(
                "SignIn",
                JsonSerializer.Serialize(userAccess));

            return Json(new
            {
                Status = 200,
                Data = "Authorized",
                UserName = userAccess.User.Name
            });
        }

        public IActionResult SignUp()
        {
            UserSignupViewModel viewModel = new();
            
            if(HttpContext.Session.Keys.Contains(RegisterKey))
            {
                UserSignupFormModel formModel =
                    JsonSerializer.Deserialize<UserSignupFormModel>(
                        HttpContext.Session.GetString(RegisterKey)!)!;

                viewModel.FormModel = formModel;
                viewModel.ValidationErrors = ValidateSignupForm(formModel);

                if(viewModel.ValidationErrors.Count == 0)
                {
                    Data.Entities.User user = new()
                    {
                        Id = Guid.NewGuid(),
                        Name = formModel.Name,
                        Email = formModel.Email,
                        Birthdate = formModel.Birthday,
                        RegisteredAt = DateTime.Now,
                        DeletedAt = null,
                    };
                    String salt = Guid.NewGuid().ToString();
                    Data.Entities.UserAccess userAccess = new()
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        RoleId = "Guest",
                        Login = formModel.Login,
                        Salt = salt,
                        Dk = _kdfService.Dk(formModel.Password, salt),
                    };
                    _dataContext.Users.Add(user);
                    _dataContext.UserAccesses.Add(userAccess);
                    try
                    {
                        _dataContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        viewModel
                            .ValidationErrors[nameof(viewModel.ValidationErrors)] = ex.Message;
                    }
                }

                HttpContext.Session.Remove(RegisterKey);
            }
            return View(viewModel);
        }


        /// Обрабатывает POST запрос на регистрацию пользователя
        /// Сохраняет данные формы в сессии и перенаправляет на форму для показа ошибок

        [HttpPost]
        public RedirectToActionResult Register(UserSignupFormModel formModel)
        {
            // Сохраняем данные формы в сессии в формате JSON
            HttpContext.Session.SetString(
                RegisterKey,
                JsonSerializer.Serialize(formModel));

            // Перенаправляем на форму для показа ошибок валидации
            return RedirectToAction(nameof(SignUp));
        }


        /// Валидирует данные формы регистрации пользователя
        /// Возвращает словарь с ошибками валидации для каждого поля

        private Dictionary<String, String> ValidateSignupForm(UserSignupFormModel formModel)
        {
            Dictionary<String, String> res = new();

            // Валідація імені
            if (String.IsNullOrEmpty(formModel.Name))
            {
                res[nameof(formModel.Name)] = "Ім'я не може бути порожнім";
            }
            else if (!char.IsUpper(formModel.Name[0]))
            {
                res[nameof(formModel.Name)] = "Ім'я повинно починатися з великої літери";
            }
            else if (!formModel.Name.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
            {
                res[nameof(formModel.Name)] = "Ім'я може містити тільки літери та пробіли";
            }

            // Валідація Email
            if (String.IsNullOrEmpty(formModel.Email))
            {
                res[nameof(formModel.Email)] = "E-mail не може бути порожнім";
            }
            else if (!IsValidEmail(formModel.Email))
            {
                res[nameof(formModel.Email)] = "Введіть правильний формат E-mail";
            }
            else if (_dataContext.Users.Any(u => u.Email == formModel.Email && u.DeletedAt == null))
            {
                res[nameof(formModel.Email)] = "Користувач з таким E-mail вже існує";
            }

            // Валідація логіну
            if (String.IsNullOrEmpty(formModel.Login))
            {
                res[nameof(formModel.Login)] = "Логін не може бути порожнім";
            }
            else if (formModel.Login.Contains(':'))
            {
                res[nameof(formModel.Login)] = "У логіні не допускається ':' (двокрапка)";
            }
            else if (formModel.Login.Length < 3)
            {
                res[nameof(formModel.Login)] = "Логін повинен містити мінімум 3 символи";
            }
            else if (_dataContext.UserAccesses.Any(ua => ua.Login == formModel.Login))
            {
                res[nameof(formModel.Login)] = "Користувач з таким логіном вже існує";
            }

            // Валідація паролю
            if (String.IsNullOrEmpty(formModel.Password))
            {
                res[nameof(formModel.Password)] = "Пароль не може бути порожнім";
            }
            else if (formModel.Password.Length < 8)
            {
                res[nameof(formModel.Password)] = "Пароль повинен містити мінімум 8 символів";
            }
            else if (!formModel.Password.Any(char.IsUpper))
            {
                res[nameof(formModel.Password)] = "Пароль повинен містити хоча б одну велику літеру";
            }
            else if (!formModel.Password.Any(char.IsDigit))
            {
                res[nameof(formModel.Password)] = "Пароль повинен містити хоча б одну цифру";
            }

            // Валідація повтору паролю
            if (formModel.Password != formModel.Repeat)
            {
                res[nameof(formModel.Repeat)] = "Паролі не збігаються";
            }

            return res;
        }


        /// Валидирует корректность email адреса
        /// Использует встроенный класс MailAddress для проверки формата

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// Валидирует имя пользователя
        private (bool IsValid, string ErrorMessage) ValidateName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return (false, "Ім'я не може бути порожнім");
            }
            
            if (!char.IsUpper(name[0]))
            {
                return (false, "Ім'я повинно починатися з великої літери");
            }
            
            if (!name.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
            {
                return (false, "Ім'я може містити тільки літери та пробіли");
            }
            
            if (name.Length > 50)
            {
                return (false, "Ім'я не може бути довшим за 50 символів");
            }
            
            return (true, string.Empty);
        }

        /// Валидирует email адрес с проверкой уникальности
        private (bool IsValid, string ErrorMessage) ValidateEmail(string email, Guid currentUserId)
        {
            if (string.IsNullOrEmpty(email))
            {
                return (false, "E-mail не може бути порожнім");
            }
            
            if (!IsValidEmail(email))
            {
                return (false, "Введіть правильний формат E-mail");
            }
            
            // Проверяем уникальность email (исключая текущего пользователя)
            if (_dataContext.Users.Any(u => u.Email == email && u.Id != currentUserId && u.DeletedAt == null))
            {
                return (false, "Користувач з таким E-mail вже існує");
            }
            
            return (true, string.Empty);
        }

    }
}

/*  Browser           НЕПРАВИЛЬНО               Server
 * POST name=User ----------------------------->  
 *     <---------------------------------------- HTML
 *  Оновити: POST name=User -------------------> ?Conflict - повторні дані
 */

/*  Browser             ПРАВИЛЬНО               Server
 * POST /Register name=User -------------------> Зберігає дані (у сесії)
 *     <------------------302------------------- Redirect /SignUp
 * GET /SignUp  -------------------------------> Відновлює та оброблює дані 
 *     <------------------200------------------- HTML
 * Оновити: GET /SignUp -----------------------> Немає конфлікту    
 */

/* Д.З. Реалізувати повну валідацію даних форми реєстрації користувача:
 * - правильність імені (починається з великої літери, не містить спецзнаки тощо)
 * - правильність E-mail
 * - вимогу до паролю (довжина, склад)
 * Вивести відповідні повідмолення на формі
 */