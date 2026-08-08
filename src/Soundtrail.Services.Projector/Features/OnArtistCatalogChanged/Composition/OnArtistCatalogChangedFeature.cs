using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Adapters.TypeRegistry;
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
        services.TryAddSingleton<ITypeRegistry>(_ => TypeTranslationRegistry.Default);

        OnArtistCatalogChangedComposition.Configure(services, new(
            sp => new RavenStoreArtistCatalogReadModelPort(sp.GetRequiredService<IDocumentStore>()),
            sp => new ArtistCatalogEventStreamRepository(
                sp.GetRequiredService<IDocumentStore>(),
                sp.GetRequiredService<ITypeRegistry>())));
    }

    public void ConfigureApplication(WebApplication app)
    {
    }
}
