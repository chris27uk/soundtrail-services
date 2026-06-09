using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Events;

public sealed record PlaybackReferencesResolutionRequired(
    MusicCatalogId MusicCatalogId,
    LookupPriorityBand Priority,
    CorrelationId CorrelationId,
    ProviderName SourceProvider,
    DateTimeOffset ObservedAt,
    PlaybackReferenceLookupKey LookupKey) : MusicTrackFact;
