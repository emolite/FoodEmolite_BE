namespace FoodEmolite.Application.DTOs.Realtime;

public class PromotionStatusChangedDto
{
    public long PromotionId { get; set; }
    public string StoreRefCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
