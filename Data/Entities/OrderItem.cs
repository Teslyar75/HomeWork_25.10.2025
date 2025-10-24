using System.ComponentModel.DataAnnotations.Schema;

namespace ASP_421.Data.Entities
{
    public class OrderItem
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }                   // Ссылка на заказ
        public Guid ProductId { get; set; }                 // Ссылка на товар
        public string ProductName { get; set; } = null!;    // Название товара (на момент заказа)
        public string? ProductDescription { get; set; }     // Описание товара (на момент заказа)
        public decimal ProductPrice { get; set; }            // Цена товара (на момент заказа)
        public int Quantity { get; set; }                   // Количество товара
        public decimal TotalPrice { get; set; }              // Общая стоимость позиции
        public string? ProductImageUrl { get; set; }        // Изображение товара (на момент заказа)
        public string ProductGroupName { get; set; } = null!; // Название группы товара (на момент заказа)
        public DateTime CreatedAt { get; set; }             // Дата создания записи

        // Навигационные свойства
        public Order Order { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
