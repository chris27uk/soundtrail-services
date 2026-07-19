using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Services.Internal.Projector.Features.OnWorkScheduled.Adapters;
using Soundtrail.Services.Internal.Projector.Infrastructure;
using Wolverine;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

namespace Soundtrail.Services.Internal.Projector.Features.OnWorkScheduled.Composition;

[Autodiscover]
public sealed class OnWorkScheduledFeature : IProjectorFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.AddWolverineCommandBus();
        services.TryAddScoped<IStoreDiscoveryFeedbackPort, RavenStoreDiscoveryFeedbackPort>();
        services.TryAddScoped<WorkScheduledProjectorHandler>();
        services.AddHostedService<WorkScheduledCdcService>();
    }

    public void ConfigureApplication(WebApplication app)
    {
    }

    public void ConfigureMessaging(WolverineOptions options, IConfiguration configuration, IHostEnvironment environment)
    {
    }
}
