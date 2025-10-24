namespace ASP_421.Models
{
    public class RepeatOrderResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<UnavailableProduct>? UnavailableProducts { get; set; }
    }

    public class UnavailableProduct
    {
        public string ProductName { get; set; } = string.Empty;
        public int RequestedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
