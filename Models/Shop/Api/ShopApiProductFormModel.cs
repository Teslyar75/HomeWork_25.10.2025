using Microsoft.AspNetCore.Mvc;

namespace ASP_421.Models.Shop.Api
{
    public class ShopApiProductFormModel
    {
        [FromForm(Name = "product-name")]
        public String Name { get; set; } = null!;

        [FromForm(Name = "product-description")]
        public String? Description { get; set; } = null!;

        [FromForm(Name = "product-slug")]
        public String? Slug { get; set; } = null!;

        [FromForm(Name = "product-image")]
        public IFormFile? Image { get; set; } = null!;

        [FromForm(Name = "product-group-id")]
        public String GroupId { get; set; } = null!;

        [FromForm(Name = "product-price")]
        public double Price { get; set; } 

        [FromForm(Name = "product-stock")]
        public int Stock { get; set; }
    }
}
