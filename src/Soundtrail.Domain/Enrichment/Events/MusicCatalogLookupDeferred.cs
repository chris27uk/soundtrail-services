using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Enrichment.Events;

public sealed record MusicCatalogLookupDeferred(
    MusicCatalogId MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset DeferredAt,
    int? RetryAfterSeconds,
    DateTimeOffset? RetryAt,
    string Reason,
    MusicSearchCriteria? SearchCriteria) : IDomainEvent;
