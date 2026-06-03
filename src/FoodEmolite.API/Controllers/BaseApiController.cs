using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FoodEmolite.API.Controllers;

public class BaseApiController : ControllerBase
{
    protected long? CurrentUserId
    {
        get
        {
            var value = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return string.IsNullOrEmpty(value)
                ? null
                : Convert.ToInt64(value);
        }
    }

    protected string? CurrentUserRefCode
    {
        get
        {
            return User.FindFirstValue(
                "ref_code");
        }
    }

    protected string? CurrentEmail
    {
        get
        {
            return User.FindFirstValue(
                ClaimTypes.Email);
        }
    }
}