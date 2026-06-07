using FoodEmolite.Application.DTOs.Store;

namespace FoodEmolite.Application.DTOs.Profile;

public class MyProfileResponseDto
{
    public AccountDto Account { get; set; }

    public AccountProfileDto? Profile { get; set; }

    public List<BankAccountDto> BankAccounts { get; set; } = [];

    public StoreResponseDto? Store { get; set; }
}

public class AccountDto
{
    public long Id { get; set; }

    public string RefCode { get; set; }

    public string Username { get; set; }

    public string Email { get; set; }

    public string Role { get; set; }

    public bool IsActive { get; set; }
}

public class AccountProfileDto
{
    public long Id { get; set; }

    public string RefCode { get; set; }

    public long AccountId { get; set; }

    public string FullName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Gender { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Address { get; set; }

    public string? AvatarUrl { get; set; }
}

public class BankAccountDto
{
    public long Id { get; set; }

    public string RefCode { get; set; }

    public long AccountId { get; set; }

    public string BankName { get; set; }

    public string? BankCode { get; set; }

    public string AccountNumber { get; set; }

    public string AccountHolderName { get; set; }

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; }
}