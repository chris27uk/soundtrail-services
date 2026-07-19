using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Soundtrail.Adapters.TypeRegistry.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTypeTranslationsFromAssemblies(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        services.TryAddSingleton<ITypeRegistry>(_ => TypeTranslationRegistry.CreateFromAssemblies(assemblies));
        return services;
    }
}
