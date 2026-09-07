using FoodEmolite.Shared.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

/// <summary>
/// Ghi nhận lượt sử dụng của khách cho promotion PRODUCT_DISCOUNT dạng "áp dụng toàn bộ sản phẩm"
/// (Promotion.ApplyToAllProducts = true) — mỗi khách (theo tài khoản đăng nhập hoặc theo Customer
/// khách vãng lai xác định qua device_id) chỉ được dùng 1 lần cho mỗi chương trình dạng này.
/// </summary>
[Table("promotion_redemptions", Schema = "food_emolite")]
public class PromotionRedemption : BaseEntity
{
    [Column("promotion_id")]
    public long PromotionId { get; set; }

    [Column("store_food_id")]
    public long StoreFoodId { get; set; }

    [Column("order_id")]
    public long OrderId { get; set; }

    // Khách đã đăng nhập
    [Column("customer_account_id")]
    public long? CustomerAccountId { get; set; }

    // Khách vãng lai (bảng customers, xác định qua device_id)
    [Column("customer_id")]
    public long? CustomerId { get; set; }
}
