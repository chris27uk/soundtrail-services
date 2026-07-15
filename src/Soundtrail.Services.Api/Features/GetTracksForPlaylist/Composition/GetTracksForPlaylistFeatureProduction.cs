using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.GetTracksForPlaylist.Adapters;
using Soundtrail.Services.Api.Features.GetTracksForPlaylist.Contract;
using Soundtrail.Services.Api.Infrastructure;

namespace Soundtrail.Services.Api.Features.GetTracksForPlaylist.Composition;

[Autodiscover]
public sealed class GetTracksForPlaylistFeatureProduction() : GetTracksForPlaylistFeature(
    _ => new SystemClockPort(),
    sp => new RavenGetTracksForPlaylistPort(CreateDocumentStore(sp), AppTypeRegistry.ServiceLocation))
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

public class GetTracksForPlaylistFeature(
    Func<IServiceProvider, IClockPort> createClockPort,
    Func<IServiceProvider, IGetTracksForPlaylistPort> createGetTracksForPlaylistPort) : IApiFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RavenDbOptions>(configuration.GetSection(RavenDbOptions.SectionName));
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
