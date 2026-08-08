using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Adapters;
using Soundtrail.Services.Internal.Projector.Infrastructure;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

namespace Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Composition;

[Autodiscover]
public sealed class OnPlaylistTracksDiscoveredFeature : IProjectorFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.AddAzureServiceBusCommandBus();

        OnPlaylistTracksDiscoveredComposition.Configure(services, new(
            sp => new RavenStorePlaylistTracksReadModelPort(sp.GetRequiredService<IDocumentStore>())));
    }

    public void ConfigureApplication(WebApplication app)
    {
    }
}
