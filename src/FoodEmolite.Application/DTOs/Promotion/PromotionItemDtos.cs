namespace FoodEmolite.Application.DTOs.Promotion;

public class PromotionFixedPriceItemRequestDto
{
    public long StoreFoodId { get; set; }
    public decimal FixedPrice { get; set; }
}

public class PromotionDiscountItemRequestDto
{
    public long StoreFoodId { get; set; }

    // PERCENT | AMOUNT
    public string DiscountType { get; set; } = "PERCENT";
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
}

public class PromotionGiftItemRequestDto
{
    public long StoreFoodId { get; set; }
    public int GiftQuantity { get; set; }
    public int SortOrder { get; set; }
}

public class PromotionFixedPriceItemResponseDto
{
    public long Id { get; set; }
    public long StoreFoodId { get; set; }
    public string FoodName { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal FixedPrice { get; set; }
}

public class PromotionDiscountItemResponseDto
{
    public long Id { get; set; }
    public long StoreFoodId { get; set; }
    public string FoodName { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public decimal OriginalPrice { get; set; }
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
}

public class PromotionGiftItemResponseDto
{
    public long Id { get; set; }
    public long StoreFoodId { get; set; }
    public string FoodName { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int GiftQuantity { get; set; }
    public int SortOrder { get; set; }
}
