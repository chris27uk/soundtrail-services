using Microsoft.Extensions.DependencyInjection;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.CompositionRoot
{
    public sealed class EnrichmentResponseFeatureOptions
    {
        public Action<IServiceCollection>? ConfigureDependencies { get; set; }

        public bool IncludeProjectionHostedService { get; set; } = true;
    }
}
