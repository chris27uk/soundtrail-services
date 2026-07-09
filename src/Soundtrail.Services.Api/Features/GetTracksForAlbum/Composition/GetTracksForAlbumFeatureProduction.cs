using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.GetTracksForAlbum.Adapters;
using Soundtrail.Services.Api.Features.GetTracksForAlbum.Contract;
using Soundtrail.Services.Api.Infrastructure;

namespace Soundtrail.Services.Api.Features.GetTracksForAlbum.Composition;

[Autodiscover]
public sealed class GetTracksForAlbumFeatureProduction() : GetTracksForAlbumFeature(
    _ => new SystemClockPort(),
    sp => new RavenGetTracksForAlbumPort(CreateDocumentStore(sp), AppTypeRegistry.ServiceLocation))
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

public class GetTracksForAlbumFeature(
    Func<IServiceProvider, IClockPort> createClockPort,
    Func<IServiceProvider, IGetTracksForAlbumPort> createGetTracksForAlbumPort) : IApiFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RavenDbOptions>(configuration.GetSection(RavenDbOptions.SectionName));
        services.Add(ServiceDescriptor.Singleton(AppTypeRegistry.ServiceLocation));
        services.TryAddScoped<IApiHandler<GetTracksForAlbumRequest, GetTracksForAlbumResponse?>, GetTracksForAlbumHandler>();
        services.Add(ServiceDescriptor.Singleton(createGetTracksForAlbumPort));
        services.Add(ServiceDescriptor.Singleton(createClockPort));
    }

    public void ConfigureApplication(IApplicationBuilder app)
    {
        app.UseEndpoints(endpoints => endpoints.MapGetTracksForAlbumEndpoints(AppTypeRegistry.ServiceLocation));
    }
}
