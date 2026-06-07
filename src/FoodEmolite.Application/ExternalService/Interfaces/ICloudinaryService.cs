using FoodEmolite.Shared.Responses;
using Microsoft.AspNetCore.Http;

namespace FoodEmolite.Application.ExternalService.Interfaces
{
    public interface ICloudinaryService
    {
        string BuildImageUrl(string publicId);
        Task<BaseResponse<string>> UploadProfileImageAsync(IFormFile file);
        Task<BaseResponse<string>> UploadProductImageAsync(IFormFile file);
        Task<BaseResponse<string>> UploadStoreImageAsync(IFormFile file);
    }
}
