using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Commands;

public sealed record ProjectMusicTrackCatalogCommand(
    MusicCatalogId MusicCatalogId,
    IReadOnlyList<VersionedMusicTrackEvent> Events);
