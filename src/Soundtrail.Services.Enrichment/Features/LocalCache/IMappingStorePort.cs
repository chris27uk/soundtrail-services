using Soundtrail.Services.Enrichment.Infrastructure.Orchestration;

namespace Soundtrail.Services.Enrichment.Features.LocalCache;

public interface IMappingStorePort
{
    Task<TrackMapping?> FindAsync(
        ResolutionDemand demand,
        CancellationToken cancellationToken);

    Task UpsertAsync(
        TrackMapping mapping,
        CancellationToken cancellationToken);
}
