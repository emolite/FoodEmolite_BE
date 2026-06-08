using FoodEmolite.Shared.Entities;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

[Table("order_items", Schema = "food_emolite")]
public class OrderItem : BaseEntity
{
    [Column("order_id")]
    public long OrderId { get; set; }

    [Column("store_food_id")]
    public long StoreFoodId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("unit_price")]
    public decimal UnitPrice { get; set; }

    [Column("total_price")]
    public decimal TotalPrice { get; set; }
}