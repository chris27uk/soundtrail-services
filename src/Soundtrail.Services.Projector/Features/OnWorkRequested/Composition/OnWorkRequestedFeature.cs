using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Services.Internal.Projector.Infrastructure;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

namespace Soundtrail.Services.Internal.Projector.Features.OnWorkRequested.Composition;

[Autodiscover]
public sealed class OnWorkRequestedFeature : IProjectorFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.AddAzureServiceBusCommandBus();
        services.TryAddScoped<WorkRequestedProjectorHandler>();
    }

    public void ConfigureApplication(WebApplication app)
    {
    }
}
