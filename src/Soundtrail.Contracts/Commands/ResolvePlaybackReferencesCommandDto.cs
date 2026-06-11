using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.Commands;

public sealed record ResolvePlaybackReferencesCommandDto(
    string CommandId,
    string MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    string CorrelationId,
    PlaybackReferenceSearchTermDto SearchTerm);
