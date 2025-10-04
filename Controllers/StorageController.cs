using ASP_421.Services.Storage;
using Microsoft.AspNetCore.Mvc;

namespace ASP_421.Controllers
{
    public class StorageController(
            IStorageService storageService
        ) : Controller
    {
        private readonly IStorageService _storageService = storageService;

        [HttpGet]
        public IActionResult Item(String id)
        {
            try
            {
                return File(_storageService.Load(id), Path.GetExtension(id) switch
                {
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    _ => "application/octet-stream"
                });
            }
            catch
            {
                return NotFound();
            }
        }
    }
}
