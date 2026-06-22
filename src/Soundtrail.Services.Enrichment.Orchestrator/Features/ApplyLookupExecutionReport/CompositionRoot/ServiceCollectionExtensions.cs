using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.Orchestrator.Features.ApplyLookupExecutionReport.Support;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.ApplyLookupExecutionReport.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplyLookupExecutionReportFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ApplyLookupExecutionReportHandler>();
        services.TryAddScoped<CatalogSearchDiscoveryByMusicCatalogIdTransitionApplier>();
        services.TryAddScoped<Adapters.LookupExecutionReportListener>();
        return services;
    }
}
