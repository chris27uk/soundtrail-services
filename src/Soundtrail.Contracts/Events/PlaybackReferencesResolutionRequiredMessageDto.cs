using Soundtrail.Contracts.Commands;
using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.Events;

public sealed record PlaybackReferencesResolutionRequiredMessageDto(
    string MusicCatalogId,
    LookupPriorityBand Priority,
    string CorrelationId,
    string SourceProvider,
    DateTimeOffset ObservedAt,
    PlaybackReferenceSearchTermDto SearchTerm);
