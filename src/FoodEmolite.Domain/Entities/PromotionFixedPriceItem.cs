using FoodEmolite.Shared.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

/// <summary>
/// Danh sách món áp dụng "Đồng giá" trong một Promotion, kèm mức giá đồng giá cho từng món.
/// </summary>
[Table("promotion_fixed_price_items", Schema = "food_emolite")]
public class PromotionFixedPriceItem : BaseEntity
{
    [Column("promotion_id")]
    public long PromotionId { get; set; }

    [Column("store_food_id")]
    public long StoreFoodId { get; set; }

    [Column("fixed_price")]
    public decimal FixedPrice { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;
}
