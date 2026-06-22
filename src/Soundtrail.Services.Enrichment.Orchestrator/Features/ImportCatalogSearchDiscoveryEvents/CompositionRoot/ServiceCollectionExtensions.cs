using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.ImportCatalogSearchDiscoveryEvents.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddImportCatalogSearchDiscoveryEventsFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ImportCatalogSearchDiscoveryEventsHandler>();
        return services;
    }
}
