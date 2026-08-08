using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Services.Api.Features.Catalog.GetArtist.Adapters;
using Soundtrail.Services.Api.Infrastructure;

namespace Soundtrail.Services.Api.Features.Catalog.GetArtist.Composition;

[Autodiscover]
public sealed class GetArtistFeatureProduction : IApiFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.Add(ServiceDescriptor.Singleton(AppTypeRegistry.ServiceLocation));

        GetArtistComposition.Configure(services, new(
            sp => new RavenGetArtistPort(sp.GetRequiredService<IDocumentStore>(), AppTypeRegistry.ServiceLocation)));
    }

    public void ConfigureApplication(WebApplication app)
    {
        app.MapGetArtistEndpoints(AppTypeRegistry.ServiceLocation);
    }
}
