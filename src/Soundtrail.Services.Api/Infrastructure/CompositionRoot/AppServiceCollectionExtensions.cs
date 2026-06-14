using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Health;
using Soundtrail.Services.Api.Features.Search;
using Soundtrail.Services.Api.Features.Search.Queueing;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using Soundtrail.Services.Api.Infrastructure.Raven;
using Soundtrail.Services.Api.Infrastructure.Time;

namespace Soundtrail.Services.Api.Infrastructure.CompositionRoot;

public static class AppServiceCollectionExtensions
{
    public static IServiceCollection AddApiAppServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        Action<ApiAppServicesOptions>? configure = null)
    {
        var options = new ApiAppServicesOptions
        {
            UseInMemoryQueueing = environment.IsEnvironment("Testing")
        };
        configure?.Invoke(options);

        options.ConfigureClockDependencies?.Invoke(services);
        services.TryAddSingleton<IClockPort, SystemClock>();

        services.AddHealthFeature();
        services.AddSearchFeature(x =>
        {
            x.ConfigureQueueingDependencies = options.ConfigureQueueingDependencies ?? (svc =>
            {
                if (options.UseInMemoryQueueing)
                {
                    svc.TryAddSingleton<IEnqueueMusicRequest, InMemoryEnqueueMusicRequest>();
                }
                else
                {
                    svc.AddLookupMusicRequestQueue(configuration);
                }
            });

            x.ConfigureCatalogSearchDependencies = options.ConfigureTrackSearchDependencies ?? (svc =>
            {
                if (environment.IsEnvironment("Testing"))
                {
                    return;
                }

                svc.AddRavenDocumentStore(configuration);
                svc.TryAddSingleton<ICatalogSearchPort, RavenCatalogSearch>();
            });
        });

        return services;
    }
}
