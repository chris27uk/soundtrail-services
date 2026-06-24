using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Adapters
{
    public sealed class MusicTrackStreamConcurrencyException(
        MusicCatalogId musicCatalogId,
        int expectedVersion,
        int actualVersion)
        : InvalidOperationException(
            $"MusicTrack stream for '{musicCatalogId.Value}' expected version {expectedVersion} but found {actualVersion}.");
}