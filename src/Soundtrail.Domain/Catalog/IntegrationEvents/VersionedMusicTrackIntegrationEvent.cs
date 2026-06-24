using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Catalog.IntegrationEvents;

public sealed record VersionedMusicTrackIntegrationEvent(
    MusicCatalogId MusicCatalogId,
    int Version,
    MusicTrackIntegrationEvent Event);
