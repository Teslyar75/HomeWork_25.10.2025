using ASP_421.Data.Entities;

namespace ASP_421.Models.Shop
{
    public class ShopAdminViewModel
    {
        public IEnumerable<ProductGroup> ProductGroups { get; set; } = [];
    }
}
