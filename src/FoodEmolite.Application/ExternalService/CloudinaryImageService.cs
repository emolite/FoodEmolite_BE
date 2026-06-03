using CloudinaryDotNet;
namespace FoodEmolite.Application.ExternalService
{
    public class CloudinaryImageService
    {
        public Cloudinary Cloudinary { get; }
        public CloudinaryImageService(Cloudinary cloudinary) { Cloudinary = cloudinary; }
    }
}
