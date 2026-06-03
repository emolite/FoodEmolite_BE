using FoodEmolite.Shared.Entities;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

[Table("bank_accounts", Schema = "food_emolite")]
public class BankAccount : BaseEntity
{
    [Column("account_id")]
    [JsonProperty("account_id")]
    public long AccountId { get; set; }

    [Column("bank_name")]
    [JsonProperty("bank_name")]
    public string BankName { get; set; }

    [Column("bank_code")]
    [JsonProperty("bank_code")]
    public string? BankCode { get; set; }

    [Column("account_number")]
    [JsonProperty("account_number")]
    public string AccountNumber { get; set; }

    [Column("account_holder_name")]
    [JsonProperty("account_holder_name")]
    public string AccountHolderName { get; set; }

    [Column("is_default")]
    [JsonProperty("is_default")]
    public bool IsDefault { get; set; }

    [Column("is_active")]
    [JsonProperty("is_active")]
    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(AccountId))]
    [JsonIgnore]
    public virtual Account? Account { get; set; }
}