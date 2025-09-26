using Ecommerce.Models.Dto;

public class OrderResponseDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = "Pending";

    public List<OrderItemResponseDto> Items { get; set; } = new();
}

public class OrderItemResponseDto
{
    public int Id { get; set; }
    public int Quantity { get; set; }

    // Embed full product details
    public ProductDto Product { get; set; } = new ProductDto();
}
