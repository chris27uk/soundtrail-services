using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogItemChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Infrastructure;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogItemChanged.Composition;

[Autodiscover]
public sealed class OnCatalogItemChangedFeature : IProjectorFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.TryAddSingleton<ITypeRegistry>(_ => TypeTranslationRegistry.Default);
        services.TryAddScoped<IEventStreamRepository<ArtistId>, ArtistCatalogEventStreamRepository>();
    }

    public void ConfigureApplication(WebApplication app)
    {
    }
}
