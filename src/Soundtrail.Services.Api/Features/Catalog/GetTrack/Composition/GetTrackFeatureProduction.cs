using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Services.Api.Features.Catalog.GetTrack.Adapters;
using Soundtrail.Services.Api.Infrastructure;

namespace Soundtrail.Services.Api.Features.Catalog.GetTrack.Composition;

[Autodiscover]
public sealed class GetTrackFeatureProduction : IApiFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.Add(ServiceDescriptor.Singleton(AppTypeRegistry.ServiceLocation));

        GetTrackComposition.Configure(services, new(
            sp => new RavenGetTrackPort(sp.GetRequiredService<IDocumentStore>(), AppTypeRegistry.ServiceLocation)));
    }

    public void ConfigureApplication(WebApplication app)
    {
        app.MapGetTrackEndpoints(AppTypeRegistry.ServiceLocation);
    }
}
