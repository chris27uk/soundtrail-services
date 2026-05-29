using Soundtrail.Services.EnrichmentWorker.Models;

namespace Soundtrail.Services.EnrichmentWorker.Ports;

public interface IMappingStorePort
{
    Task<TrackMapping?> FindAsync(
        ResolutionDemand demand,
        CancellationToken cancellationToken);

    Task UpsertAsync(
        TrackMapping mapping,
        CancellationToken cancellationToken);
}
