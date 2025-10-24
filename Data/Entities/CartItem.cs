using System.ComponentModel.DataAnnotations.Schema;

namespace ASP_421.Data.Entities
{
    public class CartItem
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }        // Ссылка на пользователя
        public Guid ProductId { get; set; }      // Ссылка на товар
        public int Quantity { get; set; }        // Количество товара
        public DateTime AddedAt { get; set; }    // Дата добавления
        public DateTime? UpdatedAt { get; set; }  // Дата последнего обновления

        // Навигационные свойства
        public User User { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
