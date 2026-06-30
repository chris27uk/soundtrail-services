using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Enrichment.Events;

public sealed record MusicCatalogLookupCompleted(
    MusicCatalogId MusicCatalogId,
    LookupSource SourceProvider,
    LookupPriorityBand Priority,
    DateTimeOffset CompletedAt,
    SongMetadata? Metadata,
    IReadOnlyList<ExternalReference> References,
    IReadOnlyList<ProviderLookupFailure> FailedProviders,
    CatalogTrackHierarchy? Hierarchy,
    MusicSearchCriteria? SearchCriteria) : IDomainEvent;
