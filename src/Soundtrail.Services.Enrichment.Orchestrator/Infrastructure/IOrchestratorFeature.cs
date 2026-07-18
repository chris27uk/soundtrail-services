using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Soundtrail.Adapters.FeatureOrchestration;
using Wolverine;

namespace Soundtrail.Services.Enrichment.Orchestrator.Infrastructure;

public interface IOrchestratorFeature : IFeature
{
    void ConfigureApplication(WebApplication app);

    void ConfigureMessaging(WolverineOptions options, IConfiguration configuration, IHostEnvironment environment);
}
