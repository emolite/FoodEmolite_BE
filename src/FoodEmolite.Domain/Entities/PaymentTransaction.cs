using FoodEmolite.Shared.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

[Table("payment_transactions", Schema = "food_emolite")]
public class PaymentTransaction : BaseEntity
{
    [Column("gateway")]
    public string? Gateway { get; set; }

    [Column("transaction_id")]
    public string? TransactionId { get; set; }

    [Column("reference_code")]
    public string? ReferenceCode { get; set; }

    [Column("account_number")]
    public string? AccountNumber { get; set; }

    [Column("transfer_type")]
    public string? TransferType { get; set; }

    [Column("transfer_amount")]
    public long TransferAmount { get; set; }

    [Column("accumulated")]
    public long? Accumulated { get; set; }

    [Column("content")]
    public string? Content { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("transaction_date")]
    public DateTime? TransactionDate { get; set; }

    [Column("raw_data")]
    public string? RawData { get; set; }

    [Column("is_processed")]
    public bool IsProcessed { get; set; }

    [Column("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    [Column("order_id")]
    public long? OrderId { get; set; }

    [Column("account_id")]
    public long? AccountId { get; set; }

    [Column("customer_id")]
    public long? CustomerId { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}