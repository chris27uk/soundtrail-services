using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Soundtrail.Translators.Registry.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTypeTranslationsFromAssemblies(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        services.TryAddSingleton<ITypeTranslator>(_ => TypeTranslationRegistry.CreateFromAssemblies(assemblies));
        return services;
    }
}
