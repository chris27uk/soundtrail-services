using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Infrastructure;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

namespace Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Composition;

[Autodiscover]
public sealed class OnArtistCatalogChangedFeature : IProjectorFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.TryAddScoped<IEventStreamRepository<ArtistId>, ArtistCatalogEventStreamRepository>();
        services.TryAddScoped<IStoreArtistCatalogReadModelPort, RavenStoreArtistCatalogReadModelPort>();
        services.TryAddScoped<ArtistCatalogChangedProjectorHandler>();
    }

    public void ConfigureApplication(WebApplication app)
    {
    }
}
