using Microsoft.AspNetCore.Mvc;

namespace ASP_421.Models.User
{
    public class UserSignupFormModel
    {
        [FromForm(Name = "user-name")]
        public String Name { get; set; } = null!;


        [FromForm(Name = "user-email")]
        public String Email { get; set; } = null!;


        [FromForm(Name = "user-login")]
        public String Login { get; set; } = null!;


        [FromForm(Name = "user-password")]
        public String Password { get; set; } = null!;


        [FromForm(Name = "user-repeat")]
        public String Repeat { get; set; } = null!;


        [FromForm(Name = "user-birthday")]
        public DateTime? Birthday { get; set; }
    }
}
/* Моделі - класи/об'єкти для передачі комплексних даних
 * - ViewModel - дані для формування представлення
 * - FormModel - об'єкт для прийому даних, частіше за все від форми
 * - Entity - моделі для подання даних з БД (або інших сховищ)
 * - інші (транзитні) - для передачі даних між сервісами
 */
