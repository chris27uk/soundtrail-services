using Microsoft.Extensions.DependencyInjection;

namespace Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution.CompositionRoot
{
    public sealed class PlaybackReferencesLookupExecutionFeatureOptions
    {
        public Action<IServiceCollection>? ConfigureDependencies { get; set; }
    }
}