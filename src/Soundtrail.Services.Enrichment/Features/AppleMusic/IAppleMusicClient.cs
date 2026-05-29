using Soundtrail.Services.Enrichment.Features.LocalCache;
using Soundtrail.Services.Enrichment.Infrastructure.Orchestration;

namespace Soundtrail.Services.Enrichment.Features.AppleMusic;

public interface IAppleMusicClient
{
    Task<TrackMapping?> TryResolveAsync(
        ResolutionDemand demand,
        CancellationToken cancellationToken);
}
