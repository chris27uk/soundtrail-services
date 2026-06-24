using Microsoft.Extensions.DependencyInjection;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupCanonicalMusicMetadata.CompositionRoot
{
    public sealed class LookupCanonicalMusicMetadataFeatureOptions
    {
        public Action<IServiceCollection>? ConfigureDependencies { get; set; }
    }
}