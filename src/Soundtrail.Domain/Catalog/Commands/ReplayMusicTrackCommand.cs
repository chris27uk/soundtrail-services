using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Commands;

public sealed record ReplayMusicTrackCommand(MusicCatalogId MusicCatalogId);
