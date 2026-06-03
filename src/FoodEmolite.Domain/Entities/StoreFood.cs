using FoodEmolite.Shared.Entities;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

[Table("store_foods", Schema = "food_emolite")]
public class StoreFood : BaseEntity
{
    [Column("store_ref_code")]
    [JsonProperty("store_ref_code")]
    public string StoreRefCode { get; set; }

    [Column("food_name")]
    [JsonProperty("food_name")]
    public string FoodName { get; set; }

    [Column("thumbnail_file_ref_code")]
    [JsonProperty("thumbnail_file_ref_code")]
    public string? ThumbnailFileRefCode { get; set; }

    [Column("description")]
    [JsonProperty("description")]
    public string? Description { get; set; }

    [Column("price")]
    [JsonProperty("price")]
    public decimal Price { get; set; }

    [Column("quantity")]
    [JsonProperty("quantity")]
    public int Quantity { get; set; }

    [Column("is_available")]
    [JsonProperty("is_available")]
    public bool IsAvailable { get; set; } = true;

    [Column("is_deleted")]
    [JsonProperty("is_deleted")]
    public bool IsDeleted { get; set; } = false;

    [ForeignKey(nameof(StoreRefCode))]
    [JsonIgnore]
    public virtual Store? Store { get; set; }
}