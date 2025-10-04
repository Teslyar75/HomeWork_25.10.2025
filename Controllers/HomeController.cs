using System.Diagnostics;
using ASP_421.Models;
using ASP_421.Services.Kdf;
using ASP_421.Services.Random;
using Microsoft.AspNetCore.Mvc;

namespace ASP_421.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IRandomService _randomService;
        private readonly IKdfService _kdfService;

        public HomeController(ILogger<HomeController> logger, IRandomService randomService, IKdfService kdfService)
        {
            _logger = logger;
            _randomService = randomService;
            _kdfService = kdfService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult IoC()
        {
            ViewData["otp"] = _kdfService.Dk("Admin", "4FA5D20B-E546-4818-9381-B4BD9F327F4E"); // _randomService.Otp(6);
            return View();
        }

        public IActionResult Razor()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Login()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
/*
 * Д.З. Реалізувати TimestampService
 * - TimestampSeconds -> дата у секундах (10 цифр)
 * - TimestampMilliseconds - у мілісекундах (13 цифр)
 * - EpochTime - як від початку ери (з 0001 року)
 * Налаштувати сервіс для автоматичного отримання
 * поточного часу при зверненні до нього
 */