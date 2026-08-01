using Microsoft.AspNetCore.Builder;
using Soundtrail.Adapters.FeatureOrchestration;

namespace Soundtrail.Services.Enrichment.Orchestrator.Infrastructure;

public interface IOrchestratorFeature : IFeature
{
    void ConfigureApplication(WebApplication app);
}
