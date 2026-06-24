using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupCanonicalMusicMetadata.Adapters;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupCanonicalMusicMetadata.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLookupCanonicalMusicMetadataFeature(
        this IServiceCollection services,
        Action<LookupCanonicalMusicMetadataFeatureOptions>? configure = null)
    {
        var options = new LookupCanonicalMusicMetadataFeatureOptions();
        configure?.Invoke(options);
        options.ConfigureDependencies?.Invoke(services);

        services.TryAddScoped<LookupCanonicalMusicMetadataHandler>();
        services.TryAddScoped<LookupCanonicalMusicMetadataListener>();
        return services;
    }
}
