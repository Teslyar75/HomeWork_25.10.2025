using Microsoft.AspNetCore.Mvc;

namespace ASP_421.Models.Shop.Api
{
    public class ShopApiGroupFormModel
    {
        [FromForm (Name = "group-name")]
        public String Name { get; set; } = null!;

        [FromForm (Name = "group-description")]
        public String Description { get; set; } = null!;


        [FromForm (Name = "group-slug")]
        public String Slug { get; set; } = null!;


        [FromForm (Name = "group-image")]
        public IFormFile Image { get; set; } = null!;

        [FromForm (Name = "parent-group")]
        public String? ParentGroup { get; set; } = null!;
    }
}
