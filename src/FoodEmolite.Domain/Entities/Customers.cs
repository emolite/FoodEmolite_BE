using FoodEmolite.Shared.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities
{
    [Table("customers", Schema = "food_emolite")]
    public class Customer : BaseEntity
    {
        [Column("customer_code")]
        public string CustomerCode { get; set; }

        [Column("customer_name")]
        public string CustomerName { get; set; }
    }
}
