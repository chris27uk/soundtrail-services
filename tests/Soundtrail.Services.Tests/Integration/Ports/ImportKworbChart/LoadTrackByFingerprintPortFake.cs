using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Tools.Operations.Features.ImportKworbChart.Ports;

namespace Soundtrail.Services.Tests.Integration.Ports.ImportKworbChart;

internal sealed class LoadTrackByFingerprintPortFake(TrackId? trackId = null) : ILoadTrackByFingerprintPort
{
    public Task<TrackId?> LoadTrackIdAsync(TrackMatchFingerprint fingerprint, CancellationToken cancellationToken) =>
        Task.FromResult(string.IsNullOrWhiteSpace(fingerprint.Value) ? null : trackId);
}
