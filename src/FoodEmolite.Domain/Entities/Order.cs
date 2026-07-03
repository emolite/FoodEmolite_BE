using FoodEmolite.Shared.Entities;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

[Table("orders", Schema = "food_emolite")]
public class Order : BaseEntity
{
    [Column("customer_account_id")]
    public long? CustomerAccountId { get; set; }

    [Column("customer_id")]
    public long? CustomerId { get; set; }

    [Column("order_code")]
    public string OrderCode { get; set; }

    [Column("store_ref_code")]
    public string StoreRefCode { get; set; }

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    [Column("order_status")]
    public string OrderStatus { get; set; }

    [Column("payment_status")]
    public string PaymentStatus { get; set; }

    [Column("note")]
    public string? Note { get; set; }

    [Column("is_delete")]
    public bool IsDelete { get; set; }
}