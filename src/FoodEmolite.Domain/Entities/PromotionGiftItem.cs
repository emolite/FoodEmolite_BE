using FoodEmolite.Shared.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

/// <summary>
/// Danh sách món được TẶNG (Y) trong Promotion kiểu "Mua X tặng Y" — do đại lý tự chọn/sắp xếp.
/// Điều kiện mua (X) dùng chung condition_type/condition_min_amount/condition_min_quantity ở bảng promotions.
/// </summary>
[Table("promotion_gift_items", Schema = "food_emolite")]
public class PromotionGiftItem : BaseEntity
{
    [Column("promotion_id")]
    public long PromotionId { get; set; }

    [Column("store_food_id")]
    public long StoreFoodId { get; set; }

    [Column("gift_quantity")]
    public int GiftQuantity { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; } = 0;

    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;
}
