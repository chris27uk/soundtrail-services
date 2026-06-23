using Microsoft.Extensions.DependencyInjection;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.BacklogScheduling.CompositionRoot
{
public sealed class BacklogSchedulingFeatureOptions
{
    public Action<IServiceCollection>? ConfigureDependencies { get; set; }
}
}
