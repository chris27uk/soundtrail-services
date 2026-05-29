using Soundtrail.Services.Enrichment.Models;

namespace Soundtrail.Services.Enrichment.Ports;

public interface IMappingStorePort
{
    Task<TrackMapping?> FindAsync(
        ResolutionDemand demand,
        CancellationToken cancellationToken);

    Task UpsertAsync(
        TrackMapping mapping,
        CancellationToken cancellationToken);
}
