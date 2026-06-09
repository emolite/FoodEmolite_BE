using FoodEmolite.Shared.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

[Table("order_item_options", Schema = "food_emolite")]
public class OrderItemOption : BaseEntity
{
    [Column("order_item_id")]
    public long OrderItemId { get; set; }

    [Column("option_group_id")]
    public long? OptionGroupId { get; set; }

    [Column("option_group_name")]
    public string OptionGroupName { get; set; }

    [Column("option_id")]
    public long? OptionId { get; set; }

    [Column("option_name")]
    public string OptionName { get; set; }

    [Column("additional_price")]
    public decimal AdditionalPrice { get; set; }
}