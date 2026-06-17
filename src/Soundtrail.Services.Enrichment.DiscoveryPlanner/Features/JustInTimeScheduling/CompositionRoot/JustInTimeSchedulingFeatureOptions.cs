using Microsoft.Extensions.DependencyInjection;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.CompositionRoot
{
    public sealed class JustInTimeSchedulingFeatureOptions
    {
        public Action<IServiceCollection>? ConfigureDependencies { get; set; }
    }
}