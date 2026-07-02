using FoodEmolite.Shared.Entities;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

[Table("user_sessions", Schema = "food_emolite")]
public class UserSession : BaseEntity
{
    [Column("id")]
    public long Id { get; set; }

    [Column("account_id")]
    public long AccountId { get; set; }

    [Column("refresh_token")]
    public string RefreshToken { get; set; }

    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [Column("user_agent")]
    public string? UserAgent { get; set; }

    [Column("device_name")]
    public string? DeviceName { get; set; }

    [Column("is_verified")]
    public bool IsVerified { get; set; }

    [Column("last_access_at")]
    public DateTime? LastAccessAt { get; set; }

    [Column("expired_at")]
    public DateTime? ExpiredAt { get; set; }

    [Column("is_actived")]
    public bool IsActived { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("created_by")]
    public long? CreatedBy { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("updated_by")]
    public long? UpdatedBy { get; set; }
}