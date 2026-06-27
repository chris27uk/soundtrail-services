using Microsoft.Extensions.DependencyInjection;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.CompositionRoot
{
    public sealed class OnSearchCatalogRequestedFeatureOptions
    {
        public Action<IServiceCollection>? ConfigureDependencies { get; set; }
    }
}
