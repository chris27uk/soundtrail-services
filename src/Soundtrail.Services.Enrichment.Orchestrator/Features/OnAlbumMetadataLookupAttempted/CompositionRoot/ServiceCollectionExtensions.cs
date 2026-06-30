using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnAlbumMetadataLookupAttempted.Adapters;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnAlbumMetadataLookupAttempted.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnAlbumMetadataLookupAttemptedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<AlbumMetadataLookupAttemptedHandler>();
        services.TryAddScoped<AlbumMetadataLookupAttemptedListener>();
        return services;
    }
}
