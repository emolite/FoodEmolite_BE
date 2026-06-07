namespace FoodEmolite.Application.DTOs.Profile;

public class UpdateBankAccountRequestDto
{
    public string BankName { get; set; }

    public string? BankCode { get; set; }

    public string AccountNumber { get; set; }

    public string AccountHolderName { get; set; }

    public bool IsDefault { get; set; }
}