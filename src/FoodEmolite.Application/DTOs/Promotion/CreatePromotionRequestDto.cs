namespace FoodEmolite.Application.DTOs.Promotion;

public class CreatePromotionRequestDto
{
    public string? PromotionCode { get; set; }

    // FIXED_PRICE | PRODUCT_DISCOUNT | BUY_X_GET_Y
    public string PromotionType { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // true = lưu nháp, không tính lịch/kích hoạt
    public bool SaveAsDraft { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    // Bitmask 7 bit: bit0=Monday ... bit6=Sunday. Mặc định áp dụng tất cả các ngày
    public int DaysOfWeekMask { get; set; } = 127;

    // NONE | MIN_ORDER_AMOUNT | MIN_QUANTITY
    public string ConditionType { get; set; } = "NONE";
    public decimal? ConditionMinAmount { get; set; }
    public int? ConditionMinQuantity { get; set; }

    public List<PromotionFixedPriceItemRequestDto> FixedPriceItems { get; set; } = new();
    public List<PromotionDiscountItemRequestDto> DiscountItems { get; set; } = new();
    public List<PromotionGiftItemRequestDto> GiftItems { get; set; } = new();
}
