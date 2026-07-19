using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Adapters.EventSourcing.CompositionRoot;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Infrastructure;
using Wolverine;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

namespace Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Composition;

[Autodiscover]
public sealed class OnArtistCatalogChangedFeature : IProjectorFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.TryAddScoped<IStoreArtistCatalogReadModelPort, RavenStoreArtistCatalogReadModelPort>();
        services.TryAddScoped<ArtistCatalogChangedProjectorHandler>();
        services.AddArtistCatalogEventStreamRepository();
        services.AddHostedService<ArtistCatalogChangedCdcService>();
    }

    public void ConfigureApplication(WebApplication app)
    {
    }

    public void ConfigureMessaging(WolverineOptions options, IConfiguration configuration, IHostEnvironment environment)
    {
    }
}
