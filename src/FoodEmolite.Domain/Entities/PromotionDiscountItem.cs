using FoodEmolite.Shared.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

/// <summary>
/// Danh sách món áp dụng "Giảm giá sản phẩm" trong một Promotion, kèm kiểu và mức giảm cho từng món.
/// </summary>
[Table("promotion_discount_items", Schema = "food_emolite")]
public class PromotionDiscountItem : BaseEntity
{
    [Column("promotion_id")]
    public long PromotionId { get; set; }

    [Column("store_food_id")]
    public long StoreFoodId { get; set; }

    // PERCENT | AMOUNT
    [Column("discount_type")]
    public string DiscountType { get; set; }

    [Column("discount_value")]
    public decimal DiscountValue { get; set; }

    // Mức giảm tối đa khi discount_type = PERCENT (null = không giới hạn)
    [Column("max_discount_amount")]
    public decimal? MaxDiscountAmount { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;
}
