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

    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;
}
