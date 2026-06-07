using FoodEmolite.Shared.Entities;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

[Table("stores", Schema = "food_emolite")]
public class Store : BaseEntity
{
    [Column("owner_account_id")]
    public long? OwnerAccountId { get; set; }

    [Column("store_name")]
    public string StoreName { get; set; }

    [Column("thumbnail_url")]
    public string? ThumbnailUrl { get; set; }

    [Column("phone_number")]
    public string? PhoneNumber { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;

    [JsonIgnore]
    public virtual ICollection<StoreFood>? StoreFoods { get; set; }
}