namespace ASP_421.Data.Entities
{
    public class ProductGroup
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }// FK на батьківську групу
        public String Name { get; set; } = null!;
        public String Description { get; set; } = null!;
        public String Slug { get; set; } = null!;// URL-адреса групи
        public String ImageUrl { get; set; } = null!;// URL-адреса зображення групи
        public DateTime? DeletedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Product> Products { get; set; } = [];
        public ProductGroup? Parent { get; set; }
    }
}
