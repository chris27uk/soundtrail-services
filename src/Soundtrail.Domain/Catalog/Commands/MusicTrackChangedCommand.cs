using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog.Projection;

namespace Soundtrail.Domain.Catalog.Commands;

public sealed record MusicTrackChangedCommand(
    MusicCatalogId MusicCatalogId,
    IReadOnlyList<VersionedMusicTrackEvent> Events);
