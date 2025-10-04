
using Microsoft.AspNetCore.Http;
using System.Data;

namespace ASP_421.Services.Storage
{

    public class DiskStorageService : IStorageService
    {
        private const String StoragePath = "wwwroot/uploads/";
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

        public byte[] Load(string filename)
        {
            if (String.IsNullOrEmpty(filename))
            {
                throw new ArgumentException("Empty file name", nameof(filename));
            }
            String savePath = Path.Combine(StoragePath, filename);
            if (File.Exists(savePath))
            {
                return File.ReadAllBytes(savePath);
            }
            else throw new FileNotFoundException();
        }

        public string Save(IFormFile formFile)
        {
            ArgumentNullException.ThrowIfNull(formFile, nameof(formFile));
            
            if (formFile.Length == 0)
            {
                throw new ArgumentException("File is empty", nameof(formFile));
            }

            if (formFile.Length > MaxFileSize)
            {
                throw new ArgumentException($"File size exceeds maximum allowed size of {MaxFileSize / (1024 * 1024)}MB", nameof(formFile));
            }

            String ext = Path.GetExtension(formFile.FileName).ToLowerInvariant();
            if (String.IsNullOrEmpty(ext))
            {
                throw new ArgumentException("File has no extension", nameof(formFile));
            }

            if (!AllowedExtensions.Contains(ext))
            {
                throw new ArgumentException($"File extension '{ext}' is not allowed. Allowed extensions: {string.Join(", ", AllowedExtensions)}", nameof(formFile));
            }

            // Создаем директорию если не существует
            if (!Directory.Exists(StoragePath))
            {
                Directory.CreateDirectory(StoragePath);
            }

            String saveName = Guid.NewGuid().ToString() + ext;
            String savePath = Path.Combine(StoragePath, saveName);
            
            using var writer = File.OpenWrite(savePath);
            formFile.CopyTo(writer);
            
            return saveName;
        }
    }
}
