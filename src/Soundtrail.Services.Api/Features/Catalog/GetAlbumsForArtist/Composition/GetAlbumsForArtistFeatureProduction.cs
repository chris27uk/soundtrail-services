using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;
using Soundtrail.Services.Api.Infrastructure;

namespace Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Composition;

[Autodiscover]
public sealed class GetAlbumsForArtistFeatureProduction : IApiFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.Add(ServiceDescriptor.Singleton(AppTypeRegistry.ServiceLocation));
        services.AddAzureServiceBusCommandBus();

        GetAlbumsForArtistComposition.Configure(services, new(
            sp => new RavenGetAlbumsForArtistPort(
                sp.GetRequiredService<IDocumentStore>(),
                AppTypeRegistry.ServiceLocation),
            _ => new SystemClockPort(),
            sp => sp.GetRequiredService<ICommandBus>(),
            sp => new RavenDiscoveryFeedbackPort(sp.GetRequiredService<IDocumentStore>())));
    }

    public void ConfigureApplication(WebApplication app)
    {
        app.MapGetAlbumsForArtistEndpoints(AppTypeRegistry.ServiceLocation);
    }
}
