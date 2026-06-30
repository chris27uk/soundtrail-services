using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Enrichment.Events;

public sealed record MusicCatalogLookupFailed(
    MusicCatalogId MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset FailedAt,
    string Reason,
    MusicSearchCriteria? SearchCriteria) : IDomainEvent;
