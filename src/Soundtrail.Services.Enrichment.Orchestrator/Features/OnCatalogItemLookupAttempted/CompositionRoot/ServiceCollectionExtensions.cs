using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogItemLookupAttempted.Adapters;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogItemLookupAttempted.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnCatalogItemLookupAttemptedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ApplyCatalogItemLookupAttemptedToArtistDiscovery>();
        services.TryAddScoped<ApplyCatalogItemLookupAttemptedToAlbumDiscovery>();
        services.TryAddScoped<CatalogItemLookupAttemptedHandler>();
        services.TryAddScoped<CatalogItemLookupAttemptedListener>();
        return services;
    }
}
