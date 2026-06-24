using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Commands;

public sealed record MusicCatalogChangedCommand(
    MusicCatalogId MusicCatalogId,
    IReadOnlyList<VersionedMusicTrackEvent> Events);
