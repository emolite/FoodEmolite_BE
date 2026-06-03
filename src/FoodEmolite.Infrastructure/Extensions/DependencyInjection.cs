using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FoodEmolite.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddAutoServices(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        var implementationTypes = assemblies
            .SelectMany(x => x.GetTypes())
            .Where(x =>
                x.IsClass &&
                !x.IsAbstract)
            .ToList();

        foreach (var implementationType in implementationTypes)
        {
            var interfaces = implementationType
                .GetInterfaces();

            foreach (var interfaceType in interfaces)
            {
                services.AddScoped(
                    interfaceType,
                    implementationType);
            }
        }

        return services;
    }
}