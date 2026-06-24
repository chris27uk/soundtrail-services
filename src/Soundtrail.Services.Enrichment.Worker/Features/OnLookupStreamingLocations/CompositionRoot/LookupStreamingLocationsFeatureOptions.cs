using Microsoft.Extensions.DependencyInjection;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.CompositionRoot
{
    public sealed class LookupStreamingLocationsFeatureOptions
    {
        public Action<IServiceCollection>? ConfigureDependencies { get; set; }
    }
}