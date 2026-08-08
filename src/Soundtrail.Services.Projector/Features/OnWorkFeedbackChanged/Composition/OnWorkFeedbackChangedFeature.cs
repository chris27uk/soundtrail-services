using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Infrastructure;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

namespace Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Composition;

[Autodiscover]
public sealed class OnWorkFeedbackChangedFeature : IProjectorFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);

        OnWorkFeedbackChangedComposition.Configure(services, new(
            sp => new RavenStoreDiscoveryFeedbackPort(sp.GetRequiredService<IDocumentStore>())));
    }

    public void ConfigureApplication(WebApplication app)
    {
    }
}
