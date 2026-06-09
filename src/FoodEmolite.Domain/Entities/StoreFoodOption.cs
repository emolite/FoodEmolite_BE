using FoodEmolite.Shared.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

[Table("store_food_options", Schema = "food_emolite")]
public class StoreFoodOption : BaseEntity
{
    [Column("option_group_id")]
    public long OptionGroupId { get; set; }

    [Column("option_name")]
    public string OptionName { get; set; }

    [Column("additional_price")]
    public decimal AdditionalPrice { get; set; }

    [Column("is_available")]
    public bool IsAvailable { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}