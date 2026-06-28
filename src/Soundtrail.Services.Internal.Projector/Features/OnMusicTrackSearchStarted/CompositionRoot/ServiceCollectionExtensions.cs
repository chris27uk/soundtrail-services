using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateRecorded.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateRecorded.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnCatalogSearchCandidateRecordedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<CatalogSearchCandidateRecordedHandler>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, CatalogSearchCandidateRecordedSubscriptionHostedService>());
        return services;
    }
}
