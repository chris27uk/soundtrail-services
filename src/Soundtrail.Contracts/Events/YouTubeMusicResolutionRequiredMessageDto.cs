using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.Events;

public sealed record YouTubeMusicResolutionRequiredMessageDto(
    string MusicCatalogId,
    LookupPriorityBand Priority,
    string CorrelationId,
    string SourceProvider,
    DateTimeOffset ObservedAt);
