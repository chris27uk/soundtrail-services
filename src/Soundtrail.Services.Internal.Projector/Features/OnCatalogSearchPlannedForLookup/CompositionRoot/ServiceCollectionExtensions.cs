using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnCatalogSearchPlannedForLookupFeature(this IServiceCollection services)
    {
        services.TryAddScoped<CatalogSearchPlannedForLookupHandler>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, CatalogSearchPlannedForLookupSubscriptionHostedService>());
        return services;
    }
}
