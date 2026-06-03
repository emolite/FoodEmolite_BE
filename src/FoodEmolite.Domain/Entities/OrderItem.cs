using FoodEmolite.Shared.Entities;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

[Table("order_items", Schema = "food_emolite")]
public class OrderItem : BaseEntity
{
    [Column("order_id")]
    public long OrderId { get; set; }

    [Column("food_ref_code")]
    public string FoodRefCode { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("unit_price")]
    public decimal UnitPrice { get; set; }

    [Column("total_price")]
    public decimal TotalPrice { get; set; }

    [ForeignKey(nameof(OrderId))]
    [JsonIgnore]
    public virtual Order? Order { get; set; }

    [ForeignKey(nameof(FoodRefCode))]
    [JsonIgnore]
    public virtual StoreFood? StoreFood { get; set; }
}