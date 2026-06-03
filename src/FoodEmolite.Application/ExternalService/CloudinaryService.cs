using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using FoodEmolite.Application.ExternalService.Interfaces;
using FoodEmolite.Shared.Responses;
using Microsoft.AspNetCore.Http;


namespace FoodEmolite.Application.ExternalService
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _imageCloudinary;

        public CloudinaryService(CloudinaryImageService imageService)
        {
            _imageCloudinary = imageService.Cloudinary;
        }

        public string BuildImageUrl(string publicId)
        {
            return _imageCloudinary.Api.UrlImgUp
                .Transform(new Transformation()
                    .Quality("auto")
                    .FetchFormat("auto"))
                .BuildUrl(publicId);
        }

        public async Task<BaseResponse<string>> UploadProfileImageAsync(IFormFile file)
        {
            var response = new BaseResponse<string>();

            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "FOOD_PROJECT/IMAGES/PROFILES"
            };

            var result = await _imageCloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
            {
                throw new Exception(result.Error.Message);
            }

            if (result.SecureUrl == null)
            {
                throw new Exception("Upload failed. SecureUrl is null.");
            }

            response.Data = result.PublicId;

            return BaseResponse<string>.Success("Created success");
        }

        public async Task<BaseResponse<string>> UploadProductImageAsync(IFormFile file)
        {
            var response = new BaseResponse<string>();

            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "FOOD_PROJECT/IMAGES/PRODUCTS"
            };

            var result = await _imageCloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
            {
                throw new Exception(result.Error.Message);
            }

            if (result.SecureUrl == null)
            {
                throw new Exception("Upload failed. SecureUrl is null.");
            }

            response.Data = result.PublicId;

            return BaseResponse<string>.Success("Created success");
        }
    }
}
