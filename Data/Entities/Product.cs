using System.ComponentModel.DataAnnotations.Schema;

namespace ASP_421.Data.Entities
{
    public class Product
    {
        public Guid Id { get; set; }
        public Guid? GroupId { get; set; }//посилання на групу
        public String Name { get; set; } = null!;
        public String? Description { get; set; } = null!;
        public String? Slug { get; set; } = null!;// URL-адреса групи

        public String? ImageUrl { get; set; } = null!;// URL-адреса зображення групи

        [Column(TypeName = "decimal(12,2)")]
        public decimal Price { get; set; }//ціна
        public int Stock { get; set; }//кількість на складі
        public DateTime? DeletedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ProductGroup Group { get; set; } = null!;






    }
}
