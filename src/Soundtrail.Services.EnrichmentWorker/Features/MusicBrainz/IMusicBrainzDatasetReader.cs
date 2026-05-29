using Soundtrail.Services.EnrichmentWorker.Models;

namespace Soundtrail.Services.EnrichmentWorker.Ports;

public interface IMusicBrainzDatasetReader
{
    Task<TrackMapping?> TryResolveAsync(
        ResolutionDemand demand,
        CancellationToken cancellationToken);
}
