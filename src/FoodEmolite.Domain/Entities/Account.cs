using FoodEmolite.Shared.Entities;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

[Table("accounts", Schema = "food_emolite")]
public class Account : BaseEntity
{
    [Column("username")]
    public string Username { get; set; }

    [Column("email")]
    public string Email { get; set; }

    [Column("password_hash")]
    public string PasswordHash { get; set; }

    [Column("role")]
    public string Role { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;

    [JsonIgnore]
    public virtual AccountProfile? Profile { get; set; }

    [JsonIgnore]
    public virtual ICollection<BankAccount>? BankAccounts { get; set; }
}