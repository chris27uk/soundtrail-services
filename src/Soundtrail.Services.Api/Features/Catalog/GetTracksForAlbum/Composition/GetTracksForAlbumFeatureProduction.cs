using Microsoft.Extensions.DependencyInjection.Extensions;
using Raven.Client.Documents;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;
using Soundtrail.Services.Api.Infrastructure;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Composition;

[Autodiscover]
public sealed class GetTracksForAlbumFeatureProduction() : GetTracksForAlbumFeature(
    _ => new SystemClockPort(),
    sp => new RavenGetTracksForAlbumPort(sp.GetRequiredService<IDocumentStore>(), AppTypeRegistry.ServiceLocation));

public class GetTracksForAlbumFeature(
    Func<IServiceProvider, IClockPort> createClockPort,
    Func<IServiceProvider, IGetTracksForAlbumPort> createGetTracksForAlbumPort) : IApiFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.Add(ServiceDescriptor.Singleton(AppTypeRegistry.ServiceLocation));
        services.TryAddScoped<IApiHandler<GetTracksForAlbumRequest, GetTracksForAlbumResponse?>, GetTracksForAlbumHandler>();
        services.Add(ServiceDescriptor.Singleton(createGetTracksForAlbumPort));
        services.Add(ServiceDescriptor.Singleton(createClockPort));
    }

    public void ConfigureApplication(WebApplication app)
    {
        app.MapGetTracksForAlbumEndpoints(AppTypeRegistry.ServiceLocation);
    }
}
