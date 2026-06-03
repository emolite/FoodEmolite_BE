using FoodEmolite.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FoodEmolite.Application.Helpers;

public static class JwtHelper
{
    public static string GenerateToken(Account account, IConfiguration configuration)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new Claim(ClaimTypes.Email, account.Email),
            new Claim("ref_code", account.RefCode)
        };

        var secretKey = configuration["Jwt:SecretKey"];

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey!));

        var credentials = new SigningCredentials(
            key, SecurityAlgorithms.HmacSha256);

        var expireDays = Convert.ToDouble(configuration["Jwt:ExpireDays"]);

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(expireDays),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static DateTime GetExpiredTime(IConfiguration configuration)
    {
        var expireDays = Convert.ToDouble(configuration["Jwt:ExpireDays"]);
        return DateTime.UtcNow.AddDays(expireDays);
    }
}