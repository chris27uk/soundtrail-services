using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnAssessMusicCatalogItem.Adapters;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnAssessMusicCatalogItem.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnAssessMusicCatalogItemFeature(this IServiceCollection services)
    {
        services.TryAddScoped<AssessMusicCatalogItemHandler>();
        services.TryAddScoped<AssessMusicCatalogItemListener>();
        return services;
    }
}
