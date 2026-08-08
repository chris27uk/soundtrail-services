using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Services.Internal.Projector.Infrastructure;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

namespace Soundtrail.Services.Internal.Projector.Features.OnWorkScheduled.Composition;

[Autodiscover]
public sealed class OnWorkScheduledFeature : IProjectorFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.AddAzureServiceBusCommandBus();
    }

    public void ConfigureApplication(WebApplication app)
    {
    }
}
