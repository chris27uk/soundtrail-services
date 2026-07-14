using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Tools.Operations.Features.ImportKworbChart.Ports;

public interface ILoadTrackByFingerprintPort
{
    Task<TrackId?> LoadTrackIdAsync(TrackMatchFingerprint fingerprint, CancellationToken cancellationToken);
}
