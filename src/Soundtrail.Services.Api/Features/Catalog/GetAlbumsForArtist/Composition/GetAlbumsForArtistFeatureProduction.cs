using Microsoft.Extensions.DependencyInjection.Extensions;
using Raven.Client.Documents;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;
using Soundtrail.Services.Api.Infrastructure;

namespace Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Composition;

[Autodiscover]
public sealed class GetAlbumsForArtistFeatureProduction() : GetAlbumsForArtistFeature(
    _ => new SystemClockPort(),
    sp => new RavenGetAlbumsForArtistPort(sp.GetRequiredService<IDocumentStore>(), AppTypeRegistry.ServiceLocation));

public class GetAlbumsForArtistFeature(
    Func<IServiceProvider, IClockPort> createClockPort,
    Func<IServiceProvider, IGetAlbumsForArtistPort> createGetAlbumsForArtistPort) : IApiFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.Add(ServiceDescriptor.Singleton(AppTypeRegistry.ServiceLocation));
        services.TryAddScoped<IApiHandler<GetAlbumsForArtistRequest, GetAlbumsForArtistResponse?>, GetAlbumsForArtistHandler>();
        services.Add(ServiceDescriptor.Singleton(createGetAlbumsForArtistPort));
        services.Add(ServiceDescriptor.Singleton(createClockPort));
    }

    public void ConfigureApplication(WebApplication app)
    {
        app.MapGetAlbumsForArtistEndpoints(AppTypeRegistry.ServiceLocation);
    }
}
