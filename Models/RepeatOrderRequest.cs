namespace ASP_421.Models
{
    public class RepeatOrderRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public List<OrderItemQuantity>? Items { get; set; }
    }

    public class OrderItemQuantity
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
