using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Internal.Projector.Features.OnMusicDataRequested;
using Soundtrail.Services.Internal.Projector.Features.OnMusicDataRequested.Adapters;
using Soundtrail.Services.Projector.Features.OnMusicDataRequested.Adapters;
using Soundtrail.Services.Projector.Infrastructure;
using Wolverine;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

namespace Soundtrail.Services.Projector.Features.OnMusicDataRequested.Composition;

[Autodiscover]
public sealed class OnMusicDataRequestedFeature : IProjectorFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.AddWolverineCommandBus();
        services.TryAddScoped<WorkRequestedProjectorHandler>();
        services.AddHostedService<CatalogSearchCandidateProjectionPollingService>();
        services.AddHostedService<WorkRequestedCdcService>();
    }

    public void ConfigureApplication(WebApplication app)
    {
    }

    public void ConfigureMessaging(WolverineOptions options, IConfiguration configuration, IHostEnvironment environment)
    {
    }
}
