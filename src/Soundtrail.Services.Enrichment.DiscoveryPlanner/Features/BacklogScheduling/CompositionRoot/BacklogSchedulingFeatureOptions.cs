using Microsoft.Extensions.DependencyInjection;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling.CompositionRoot
{
    public sealed class BacklogSchedulingFeatureOptions
    {
        public Action<IServiceCollection>? ConfigureDependencies { get; set; }

        public bool IncludeHostedService { get; set; } = true;
    }
}