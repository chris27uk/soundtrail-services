using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnArtistMetadataLookupAttempted.Adapters;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnArtistMetadataLookupAttempted.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnArtistMetadataLookupAttemptedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ArtistMetadataLookupAttemptedHandler>();
        services.TryAddScoped<ArtistMetadataLookupAttemptedListener>();
        return services;
    }
}
