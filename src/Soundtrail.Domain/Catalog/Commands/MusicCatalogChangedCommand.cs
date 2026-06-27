using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog.Projection;

namespace Soundtrail.Domain.Catalog.Commands;

public sealed record MusicCatalogChangedCommand(MusicCatalogId MusicCatalogId, IReadOnlyList<VersionedMusicTrackEvent> Events);
