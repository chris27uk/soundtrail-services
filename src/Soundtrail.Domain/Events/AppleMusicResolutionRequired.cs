using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Events;

public sealed record AppleMusicResolutionRequired(
    MusicCatalogId MusicCatalogId,
    LookupPriorityBand Priority,
    CorrelationId CorrelationId,
    ProviderName SourceProvider,
    DateTimeOffset ObservedAt) : MusicTrackFact;
