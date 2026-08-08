using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Infrastructure;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged.Composition;

[Autodiscover]
public sealed class OnCatalogSearchCandidateChangedFeature : IProjectorFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);

        OnCatalogSearchCandidateChangedComposition.Configure(services, new(
            sp => new RavenStoreCatalogSearchCandidatePort(sp.GetRequiredService<IDocumentStore>())));
    }

    public void ConfigureApplication(WebApplication app)
    {
    }
}
