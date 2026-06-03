using CloudinaryDotNet;
using FoodEmolite.Application.DTOs.Cloudinary;
using FoodEmolite.Application.ExternalService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodEmolite.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCloudinaryConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<CloudinarySettingsDto>()
                .Bind(configuration.GetSection("CloudinarySettingsImg"));

            services.AddSingleton<CloudinaryImageService>(provider =>
            {
                var settings = provider
                    .GetRequiredService<IOptions<CloudinarySettingsDto>>()
                    .Value;

                var account = new Account(
                    settings.CloudName,
                    settings.ApiKey,
                    settings.ApiSecret
                );

                return new CloudinaryImageService(
                    new Cloudinary(account));
            });

            return services;
        }
    }
}
    