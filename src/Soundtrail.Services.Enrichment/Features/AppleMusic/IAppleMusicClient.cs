using Soundtrail.Services.Enrichment.Models;

namespace Soundtrail.Services.Enrichment.Ports;

public interface IAppleMusicClient
{
    Task<TrackMapping?> TryResolveAsync(
        ResolutionDemand demand,
        CancellationToken cancellationToken);
}
