using Microsoft.Extensions.DependencyInjection.Extensions;
using Raven.Client.Documents;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;
using Soundtrail.Services.Api.Infrastructure;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Composition;

[Autodiscover]
public sealed class GetTracksForPlaylistFeatureProduction() : GetTracksForPlaylistFeature(
    _ => new SystemClockPort(),
    sp => new RavenGetTracksForPlaylistPort(sp.GetRequiredService<IDocumentStore>(), AppTypeRegistry.ServiceLocation));

public class GetTracksForPlaylistFeature(
    Func<IServiceProvider, IClockPort> createClockPort,
    Func<IServiceProvider, IGetTracksForPlaylistPort> createGetTracksForPlaylistPort) : IApiFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.Add(ServiceDescriptor.Singleton(AppTypeRegistry.ServiceLocation));
        services.TryAddScoped<IApiHandler<GetTracksForPlaylistRequest, GetTracksForPlaylistResponse?>, GetTracksForPlaylistHandler>();
        services.Add(ServiceDescriptor.Singleton(createGetTracksForPlaylistPort));
        services.Add(ServiceDescriptor.Singleton(createClockPort));
    }

    public void ConfigureApplication(WebApplication app)
    {
        app.MapGetTracksForPlaylistEndpoints(AppTypeRegistry.ServiceLocation);
    }
}
