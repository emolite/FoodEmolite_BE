namespace FoodEmolite.Application.DTOs.Auth;

public class CurrentUserResponse
{
    public long Id { get; set; }

    public string RefCode { get; set; }

    public string Username { get; set; }

    public string Email { get; set; }

    public string Role { get; set; }
}