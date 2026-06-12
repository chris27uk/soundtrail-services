using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup.Adapters;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnDemandMetadataLookupFeature(
        this IServiceCollection services,
        Action<OnDemandMetadataLookupFeatureOptions>? configure = null)
    {
        var options = new OnDemandMetadataLookupFeatureOptions();
        configure?.Invoke(options);
        options.ConfigureDependencies?.Invoke(services);

        services.TryAddScoped<OnDemandLookupMetadataHandler>();
        services.TryAddScoped<MusicBrainzLookupExecutionListener>();
        return services;
    }
}
