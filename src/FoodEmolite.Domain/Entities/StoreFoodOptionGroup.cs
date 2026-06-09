using FoodEmolite.Shared.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

[Table("store_food_option_groups", Schema = "food_emolite")]
public class StoreFoodOptionGroup : BaseEntity
{
    [Column("store_food_id")]
    public long StoreFoodId { get; set; }

    [Column("group_name")]
    public string GroupName { get; set; }

    [Column("is_required")]
    public bool IsRequired { get; set; }

    [Column("min_select")]
    public int MinSelect { get; set; }

    [Column("max_select")]
    public int MaxSelect { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}