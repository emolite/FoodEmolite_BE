namespace FoodEmolite.Application.DTOs.Realtime;

public class NewOrderNotificationDto
{
    public long OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string StoreRefCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
