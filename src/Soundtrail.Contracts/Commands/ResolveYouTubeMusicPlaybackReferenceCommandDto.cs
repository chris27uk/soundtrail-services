using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.Commands;

public sealed record ResolveYouTubeMusicPlaybackReferenceCommandDto(
    string CommandId,
    string MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    string CorrelationId);
