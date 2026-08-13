namespace FoodEmolite.Application.DTOs.Realtime;

public class FoodQuantityChangedDto
{
    public long StoreFoodId { get; set; }
    public string StoreRefCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
