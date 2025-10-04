using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ASP_421.Services.Storage
{
    public interface IStorageService
    {
        String Save(IFormFile formFile);// returns ImageUrl (filename)
        byte[] Load(String filename);

    }
}

