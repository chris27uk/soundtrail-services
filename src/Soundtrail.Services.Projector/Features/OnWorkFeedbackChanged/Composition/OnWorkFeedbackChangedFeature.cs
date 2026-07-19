using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Infrastructure;
using Wolverine;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

namespace Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Composition;

[Autodiscover]
public sealed class OnWorkFeedbackChangedFeature : IProjectorFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.TryAddScoped<IStoreDiscoveryFeedbackPort, RavenStoreDiscoveryFeedbackPort>();
        services.TryAddScoped<WorkFeedbackChangedProjectorHandler>();
        services.AddHostedService<WorkFeedbackChangedCdcService>();
    }

    public void ConfigureApplication(WebApplication app)
    {
    }

    public void ConfigureMessaging(WolverineOptions options, IConfiguration configuration, IHostEnvironment environment)
    {
    }
}
