using Microsoft.Extensions.DependencyInjection.Extensions;
using Raven.Client.Documents;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Contract;
using Soundtrail.Services.Api.Infrastructure;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Composition;

[Autodiscover]
public sealed class GetTracksForArtistFeatureProduction() : GetTracksForArtistFeature(
    _ => new SystemClockPort(),
    sp => new RavenGetTracksForArtistPort(sp.GetRequiredService<IDocumentStore>(), AppTypeRegistry.ServiceLocation));

public class GetTracksForArtistFeature(
    Func<IServiceProvider, IClockPort> createClockPort,
    Func<IServiceProvider, IGetTracksForArtistPort> createGetTracksForArtistPort) : IApiFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.Add(ServiceDescriptor.Singleton(AppTypeRegistry.ServiceLocation));
        services.TryAddScoped<IApiHandler<GetTracksForArtistRequest, GetTracksForArtistResponse?>, GetTracksForArtistHandler>();
        services.Add(ServiceDescriptor.Singleton(createGetTracksForArtistPort));
        services.Add(ServiceDescriptor.Singleton(createClockPort));
    }

    public void ConfigureApplication(WebApplication app)
    {
        app.MapGetTracksForArtistEndpoints(AppTypeRegistry.ServiceLocation);
    }
}
