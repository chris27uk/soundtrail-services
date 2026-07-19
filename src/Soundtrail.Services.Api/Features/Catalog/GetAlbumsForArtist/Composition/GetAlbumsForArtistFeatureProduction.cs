using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
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
    sp => new RavenGetAlbumsForArtistPort(CreateDocumentStore(sp), AppTypeRegistry.ServiceLocation))
{
    private static IDocumentStore CreateDocumentStore(IServiceProvider sp)
    {
        var options = sp.GetRequiredService<IOptions<RavenDbOptions>>().Value;
        var store = new DocumentStore
        {
            Urls = options.Urls,
            Database = options.Database,
            Conventions = new DocumentConventions
            {
                FindCollectionName = type => type.Name
            }
        };

        return store.Initialize();
    }
}

public class GetAlbumsForArtistFeature(
    Func<IServiceProvider, IClockPort> createClockPort,
    Func<IServiceProvider, IGetAlbumsForArtistPort> createGetAlbumsForArtistPort) : IApiFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RavenDbOptions>(configuration.GetSection(RavenDbOptions.SectionName));
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
