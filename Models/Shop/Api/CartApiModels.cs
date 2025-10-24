namespace ASP_421.Models.Shop.Api
{
    public class CartApiResponse
    {
        public string Status { get; set; } = "Ok";
        public string? ErrorMessage { get; set; }
        public object? Data { get; set; }
    }

    public class CartItemApiModel
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string? ProductDescription { get; set; }
        public string? ProductImageUrl { get; set; }
        public decimal ProductPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime AddedAt { get; set; }
    }

    public class CartSummaryApiModel
    {
        public int ItemsCount { get; set; }
        public decimal TotalAmount { get; set; }
        public List<CartItemApiModel> Items { get; set; } = new();
    }

    public class AddToCartRequest
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class UpdateCartItemRequest
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
