using Microsoft.Extensions.DependencyInjection;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup.CompositionRoot
{
    public sealed class OnDemandMetadataLookupFeatureOptions
    {
        public Action<IServiceCollection>? ConfigureDependencies { get; set; }
    }
}