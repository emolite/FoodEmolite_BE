using FoodEmolite.Shared.Entities;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

[Table("order_histories", Schema = "food_emolite")]
public class OrderHistory : BaseEntity
{
    [Column("order_id")]
    [JsonProperty("order_id")]
    public long OrderId { get; set; }

    [Column("old_status")]
    [JsonProperty("old_status")]
    public string? OldStatus { get; set; }

    [Column("new_status")]
    [JsonProperty("new_status")]
    public string NewStatus { get; set; }

    [Column("changed_note")]
    [JsonProperty("changed_note")]
    public string? ChangedNote { get; set; }

    [ForeignKey(nameof(OrderId))]
    [JsonIgnore]
    public virtual Order? Order { get; set; }
}