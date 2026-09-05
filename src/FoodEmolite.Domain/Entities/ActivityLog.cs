using FoodEmolite.Shared.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

/// <summary>
/// Nhật ký hoạt động toàn hệ thống FoodEmolite - ai đã làm gì, lúc nào.
/// Ghi nhận riêng (không chỉ đọc lại từ Order/OrderHistory) để có 1 nguồn duy nhất,
/// dễ truy vấn cho màn Lịch sử hoạt động bên admin MusicEmolite.
/// </summary>
[Table("activity_logs", Schema = "food_emolite")]
public class ActivityLog : BaseEntity
{
    /// <summary>"Customer" (tài khoản đã đăng ký), "Guest" (khách vãng lai), "Agent", hoặc "System".</summary>
    [Column("actor_type")]
    public string ActorType { get; set; } = string.Empty;

    /// <summary>Id trong bảng tương ứng với ActorType (Account.Id nếu Customer/Agent, Customer.Id nếu Guest). Null nếu System.</summary>
    [Column("actor_id")]
    public long? ActorId { get; set; }

    /// <summary>Tên hiển thị của actor, lưu sẵn để không phải join lại khi hiển thị log.</summary>
    [Column("actor_name")]
    public string? ActorName { get; set; }

    /// <summary>Mã hành động: CREATE_STORE, CREATE_AGENT, CREATE_ORDER, CONFIRM_PAYMENT,...</summary>
    [Column("action")]
    public string Action { get; set; } = string.Empty;

    [Column("description")]
    public string Description { get; set; } = string.Empty;
}
