using Microsoft.Extensions.DependencyInjection;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnNextMusicTracksRequestedForLookup.CompositionRoot
{
public sealed class OnNextMusicTracksRequestedForLookupFeatureOptions
{
    public Action<IServiceCollection>? ConfigureDependencies { get; set; }
}
}
