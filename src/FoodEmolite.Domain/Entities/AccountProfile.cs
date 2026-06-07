using FoodEmolite.Shared.Entities;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

[Table("account_profiles", Schema = "food_emolite")]
public class AccountProfile : BaseEntity
{
    [Column("account_id")]
    public long AccountId { get; set; }

    [Column("full_name")]
    public string FullName { get; set; }

    [Column("phone_number")]
    public string? PhoneNumber { get; set; }

    [Column("gender")]
    public string? Gender { get; set; }

    [Column("date_of_birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }
}