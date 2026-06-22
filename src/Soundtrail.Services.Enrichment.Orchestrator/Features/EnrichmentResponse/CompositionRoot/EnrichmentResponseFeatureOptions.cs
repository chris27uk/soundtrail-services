using Microsoft.Extensions.DependencyInjection;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.EnrichmentResponse.CompositionRoot
{
    public sealed class EnrichmentResponseFeatureOptions
    {
        public Action<IServiceCollection>? ConfigureDependencies { get; set; }
    }
}
