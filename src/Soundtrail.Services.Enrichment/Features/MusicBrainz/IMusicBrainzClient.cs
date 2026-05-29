using Soundtrail.Services.Enrichment.Features.LocalCache;
using Soundtrail.Services.Enrichment.Infrastructure.Orchestration;

namespace Soundtrail.Services.Enrichment.Features.MusicBrainz;

public interface IMusicBrainzClient
{
    Task<TrackMapping?> TryResolveAsync(
        ResolutionDemand demand,
        CancellationToken cancellationToken);
}
