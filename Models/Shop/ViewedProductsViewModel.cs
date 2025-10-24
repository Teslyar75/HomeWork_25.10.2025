using ASP_421.Data.Entities;

namespace ASP_421.Models.Shop
{
    public class ViewedProductsViewModel
    {
        public IEnumerable<Product> Products { get; set; } = new List<Product>();
        public int MaxItems { get; set; } = 4;
        public string Title { get; set; } = "Нещодавно переглянуті товари";
        public bool ShowOnCartPage { get; set; } = true;
        public bool ShowOnHomePage { get; set; } = true;
        public bool ShowOnProductPage { get; set; } = true;
    }
}
