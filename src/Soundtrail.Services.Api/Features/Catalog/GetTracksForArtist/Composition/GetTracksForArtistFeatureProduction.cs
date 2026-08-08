using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;
using Soundtrail.Services.Api.Infrastructure;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Composition;

[Autodiscover]
public sealed class GetTracksForArtistFeatureProduction : IApiFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.Add(ServiceDescriptor.Singleton(AppTypeRegistry.ServiceLocation));
        services.AddAzureServiceBusCommandBus();

        GetTracksForArtistComposition.Configure(services, new(
            sp => new RavenGetTracksForArtistPort(
                sp.GetRequiredService<IDocumentStore>(),
                AppTypeRegistry.ServiceLocation),
            _ => new SystemClockPort(),
            sp => sp.GetRequiredService<ICommandBus>(),
            sp => new RavenDiscoveryFeedbackPort(sp.GetRequiredService<IDocumentStore>())));
    }

    public void ConfigureApplication(WebApplication app)
    {
        app.MapGetTracksForArtistEndpoints(AppTypeRegistry.ServiceLocation);
    }
}
