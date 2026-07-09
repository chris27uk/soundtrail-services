using Soundtrail.Domain.Catalog;

namespace Soundtrail.Tools.Operations.Features.ImportKworbChart.Ports;

public interface ILoadTrackByFingerprintPort
{
    Task<TrackId?> LoadTrackIdAsync(TrackMatchFingerprint fingerprint, CancellationToken cancellationToken);
}
