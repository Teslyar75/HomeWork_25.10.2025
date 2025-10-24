using System.ComponentModel.DataAnnotations.Schema;

namespace ASP_421.Data.Entities
{
    public class Order
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }                    // Ссылка на пользователя
        public DateTime OrderDate { get; set; }             // Дата заказа
        public decimal TotalAmount { get; set; }            // Общая сумма заказа
        public int ItemsCount { get; set; }                 // Количество товаров
        public string Status { get; set; } = "Completed";   // Статус заказа
        public DateTime? CompletedAt { get; set; }          // Дата завершения
        public DateTime CreatedAt { get; set; }             // Дата создания записи
        public DateTime? UpdatedAt { get; set; }            // Дата обновления

        // Навигационные свойства
        public User User { get; set; } = null!;
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
