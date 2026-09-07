namespace FoodEmolite.Application.DTOs.Promotion;

public class PromotionResponseDto
{
    public long Id { get; set; }
    public string? RefCode { get; set; }
    public string StoreRefCode { get; set; } = string.Empty;
    public string? PromotionCode { get; set; }
    public string PromotionType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int DaysOfWeekMask { get; set; }

    public string ConditionType { get; set; } = string.Empty;
    public decimal? ConditionMinAmount { get; set; }
    public int? ConditionMinQuantity { get; set; }

    public bool ApplyToAllProducts { get; set; }
    public string? DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<PromotionFixedPriceItemResponseDto> FixedPriceItems { get; set; } = new();
    public List<PromotionDiscountItemResponseDto> DiscountItems { get; set; } = new();
    public List<PromotionGiftItemResponseDto> GiftItems { get; set; } = new();
}
