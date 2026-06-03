using FoodEmolite.Application.DTOs.Auth;
using FoodEmolite.Shared.Responses;

namespace FoodEmolite.Application.Interfaces
{
    public interface IAuthService
    {
        Task<BaseResponse<string>>RegisterAsync(RegisterRequest request);

        Task<BaseResponse<LoginResponse>>LoginAsync(LoginRequest request);

        Task<BaseResponse<CurrentUserResponse>>VerifyAsync(long userId);

        Task<BaseResponse<bool>> CheckEmailAsync(string email);
    }
}
