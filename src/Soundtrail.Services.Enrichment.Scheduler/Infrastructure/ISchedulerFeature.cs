using Microsoft.AspNetCore.Builder;
using Soundtrail.Adapters.FeatureOrchestration;

namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure;

public interface ISchedulerFeature : IFeature
{
    void ConfigureApplication(WebApplication app);
}
