using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Adapters.EventSourcing.CompositionRoot;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogItemChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Infrastructure;
using Wolverine;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogItemChanged.Composition;

[Autodiscover]
public sealed class OnCatalogItemChangedFeature : IProjectorFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.TryAddSingleton<ITypeRegistry>(_ => TypeTranslationRegistry.Default);
        services.TryAddScoped<CatalogItemChangedProjectorHandler>();
        services.AddArtistCatalogEventStreamRepository();
        services.AddHostedService<CatalogItemChangedCdcService>();
    }

    public void ConfigureApplication(WebApplication app)
    {
    }

    public void ConfigureMessaging(WolverineOptions options, IConfiguration configuration, IHostEnvironment environment)
    {
    }
}
