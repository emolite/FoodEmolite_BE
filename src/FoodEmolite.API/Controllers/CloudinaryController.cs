using FoodEmolite.Application.ExternalService.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FoodEmolite.API.Controllers
{
    [ApiController]
    [Route("api/cloudinary")]
    public class CloudinaryController : BaseApiController
    {
        private readonly ICloudinaryService _cloudinaryService;

        public CloudinaryController(ICloudinaryService cloudinaryService)
        {
            _cloudinaryService = cloudinaryService;
        }

        [HttpPost("upload-profile-image")]
        public async Task<IActionResult> UploadProfileImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is required");
            }

            var result = await _cloudinaryService.UploadProfileImageAsync(file);

            return Ok(result);
        }

        [HttpPost("upload-product-image")]
        public async Task<IActionResult> UploadMusicImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is required");
            }

            var result = await _cloudinaryService.UploadProductImageAsync(file);

            return Ok(result);
        }

        [HttpPost("upload-store-image")]
        public async Task<IActionResult> UploadStoreImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            var result = await _cloudinaryService.UploadStoreImageAsync(file);

            return Ok(result);
        }
    }
}
