using FoodEmolite.Shared.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Domain.Entities
{
    [Table("store_food_categories", Schema = "food_emolite")]
    public class StoreFoodCategories : BaseEntity
    {
        [Column("store_ref_code")]
        public string StoreRefCode { get; set; }

        [Column("category_name")]
        public string CategoryName { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("is_delete")]
        public bool IsDelete { get; set; } = false;
    }
}
