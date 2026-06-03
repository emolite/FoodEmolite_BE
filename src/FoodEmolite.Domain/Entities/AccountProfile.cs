using FoodEmolite.Shared.Entities;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

[Table("account_profiles", Schema = "food_emolite")]
public class AccountProfile : BaseEntity
{
    [Column("account_id")]
    [JsonProperty("account_id")]
    public long AccountId { get; set; }

    [Column("full_name")]
    [JsonProperty("full_name")]
    public string FullName { get; set; }

    [Column("phone_number")]
    [JsonProperty("phone_number")]
    public string? PhoneNumber { get; set; }

    [Column("gender")]
    [JsonProperty("gender")]
    public string? Gender { get; set; }

    [Column("date_of_birth")]
    [JsonProperty("date_of_birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Column("address")]
    [JsonProperty("address")]
    public string? Address { get; set; }

    [Column("avatar_file_ref_code")]
    [JsonProperty("avatar_file_ref_code")]
    public string? AvatarFileRefCode { get; set; }

    [ForeignKey(nameof(AccountId))]
    [JsonIgnore]
    public virtual Account? Account { get; set; }
}