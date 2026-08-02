using Microsoft.Extensions.DependencyInjection.Extensions;
using Raven.Client.Documents;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.GetAlbum.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetAlbum.Contract;
using Soundtrail.Services.Api.Infrastructure;

namespace Soundtrail.Services.Api.Features.Catalog.GetAlbum.Composition
{
    // Only the ports are injected here, everything else is shared between app and tests
    [Autodiscover]
    public class GetAlbumFeatureProduction() : GetAlbumFeature(
        _ => new SystemClockPort(),
        sp => new RavenGetAlbumPort(sp.GetRequiredService<IDocumentStore>(), AppTypeRegistry.ServiceLocation));

    public class GetAlbumFeature(Func<IServiceProvider, IClockPort> createClockPort, Func<IServiceProvider, IGetAlbumPort> createGetAlbumPort) : IApiFeature 
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddRavenDocumentStore(configuration);
            services.Add(ServiceDescriptor.Singleton(AppTypeRegistry.ServiceLocation));
            services.TryAddScoped<IApiHandler<GetAlbumRequest, GetAlbumResponse?>, GetAlbumHandler>();
            services.Add(ServiceDescriptor.Singleton(createGetAlbumPort));
            services.Add(ServiceDescriptor.Singleton(createClockPort));
        }

        public void ConfigureApplication(WebApplication app)
        {
            app.MapGetAlbumEndpoints(AppTypeRegistry.ServiceLocation);
        }
    }
}
