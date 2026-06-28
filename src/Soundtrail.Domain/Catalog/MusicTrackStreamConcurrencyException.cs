using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Catalog;

public sealed class MusicTrackStreamConcurrencyException(
    MusicCatalogId musicCatalogId,
    int expectedVersion,
    int actualVersion)
    : InvalidOperationException(
        $"MusicTrack stream for '{musicCatalogId.Value}' expected version {expectedVersion} but found {actualVersion}.");
