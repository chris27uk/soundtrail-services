using Microsoft.Extensions.DependencyInjection;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupTrackMetadata.CompositionRoot;

public sealed class LookupTrackMetadataFeatureOptions
{
    public Action<IServiceCollection>? ConfigureDependencies { get; set; }
}
