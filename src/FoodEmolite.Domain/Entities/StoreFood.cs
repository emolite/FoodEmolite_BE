using FoodEmolite.Shared.Entities;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

[Table("store_foods", Schema = "food_emolite")]
public class StoreFood : BaseEntity
{
    [Column("store_ref_code")]
    public string StoreRefCode { get; set; }

    [Column("food_name")]
    public string FoodName { get; set; }

    [Column("thumbnail_url")]
    public string? ThumbnailUrl { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("price")]
    public decimal Price { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("is_available")]
    public bool IsAvailable { get; set; } = true;

    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;

    [ForeignKey(nameof(StoreRefCode))]
    [JsonIgnore]
    public virtual Store? Store { get; set; }
}