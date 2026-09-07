using FoodEmolite.Shared.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

[Table("promotions", Schema = "food_emolite")]
public class Promotion : BaseEntity
{
    [Column("store_ref_code")]
    public string StoreRefCode { get; set; }

    [Column("promotion_code")]
    public string? PromotionCode { get; set; }

    // FIXED_PRICE | PRODUCT_DISCOUNT | BUY_X_GET_Y
    [Column("promotion_type")]
    public string PromotionType { get; set; }

    [Column("name")]
    public string Name { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    // DRAFT | SCHEDULED | ACTIVE | PAUSED | ENDED
    [Column("status")]
    public string Status { get; set; } = "DRAFT";

    [Column("start_date")]
    public DateOnly StartDate { get; set; }

    // null = không có ngày kết thúc
    [Column("end_date")]
    public DateOnly? EndDate { get; set; }

    // null = áp dụng cả ngày (không giới hạn theo khung giờ)
    [Column("start_time")]
    public TimeOnly? StartTime { get; set; }

    [Column("end_time")]
    public TimeOnly? EndTime { get; set; }

    // Bitmask 7 bit: bit0=Monday ... bit6=Sunday. 127 = áp dụng tất cả các ngày trong tuần
    [Column("days_of_week_mask")]
    public int DaysOfWeekMask { get; set; } = 127;

    // NONE | MIN_ORDER_AMOUNT | MIN_QUANTITY
    [Column("condition_type")]
    public string ConditionType { get; set; } = "NONE";

    [Column("condition_min_amount")]
    public decimal? ConditionMinAmount { get; set; }

    [Column("condition_min_quantity")]
    public int? ConditionMinQuantity { get; set; }

    // Chỉ dùng khi PromotionType = PRODUCT_DISCOUNT: true = giảm giá áp dụng cho TOÀN BỘ sản phẩm
    // của cửa hàng, khách tự chọn 1 món bất kỳ trong đơn để nhận giảm giá lúc thanh toán (thay vì
    // agent chọn sẵn danh sách món qua PromotionDiscountItem). Mỗi khách chỉ được dùng 1 lần
    // (xem PromotionRedemption).
    [Column("apply_to_all_products")]
    public bool ApplyToAllProducts { get; set; } = false;

    // PERCENT | AMOUNT — dùng khi ApplyToAllProducts = true
    [Column("discount_type")]
    public string? DiscountType { get; set; }

    [Column("discount_value")]
    public decimal? DiscountValue { get; set; }

    [Column("max_discount_amount")]
    public decimal? MaxDiscountAmount { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;
}
