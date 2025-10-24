using ASP_421.Data.Entities;

namespace ASP_421.Models.User
{
    public class UserProfileViewModel
    {
        public Data.Entities.User? User { get; set; }
        
        public bool IsPersonal { get; set; }
        
        // Данные корзины
        public IEnumerable<CartItem>? CartItems { get; set; }
        public int CartItemsCount { get; set; }
        public decimal CartTotalAmount { get; set; }
        
        // Статистика пользователя
        public int TotalOrdersCount { get; set; }
        public decimal TotalSpentAmount { get; set; }
        public DateTime? LastOrderDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        
        // Активность
        public int DaysSinceRegistration { get; set; }
        public bool IsActiveUser { get; set; }
    }
}
