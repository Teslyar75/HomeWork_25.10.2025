using System.ComponentModel.DataAnnotations;

namespace ASP_421.Models.Shop
{
    public class EditGroupViewModel
    {
        public Guid Id { get; set; }
        
        [Required(ErrorMessage = "Назва групи обов'язкова")]
        [StringLength(100, ErrorMessage = "Назва групи не може перевищувати 100 символів")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500, ErrorMessage = "Опис групи не може перевищувати 500 символів")]
        public string? Description { get; set; }
        
        public string? ImageUrl { get; set; }
        
        public Guid? ParentId { get; set; }
        
        public string Slug { get; set; } = string.Empty;
        
        public bool IsDeleted { get; set; }
        
        // Для отображения
        public string? ParentName { get; set; }
        public int ProductsCount { get; set; }
        public int SubGroupsCount { get; set; }
    }

    public class EditProductViewModel
    {
        public Guid Id { get; set; }
        
        [Required(ErrorMessage = "Назва товару обов'язкова")]
        [StringLength(100, ErrorMessage = "Назва товару не може перевищувати 100 символів")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(1000, ErrorMessage = "Опис товару не може перевищувати 1000 символів")]
        public string? Description { get; set; }
        
        [Required(ErrorMessage = "Ціна товару обов'язкова")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Ціна повинна бути більше 0")]
        public decimal Price { get; set; }
        
        [Range(0, int.MaxValue, ErrorMessage = "Кількість на складі не може бути від'ємною")]
        public int Stock { get; set; }
        
        public string? ImageUrl { get; set; }
        
        [Required(ErrorMessage = "Група товару обов'язкова")]
        public Guid GroupId { get; set; }
        
        public string Slug { get; set; } = string.Empty;
        
        public bool IsDeleted { get; set; }
        
        // Для отображения
        public string? GroupName { get; set; }
    }

    public class AdminEditViewModel
    {
        public List<EditGroupViewModel> Groups { get; set; } = new();
        public List<EditProductViewModel> Products { get; set; } = new();
        public List<EditGroupViewModel> AllGroups { get; set; } = new(); // Для выпадающих списков
    }
}
