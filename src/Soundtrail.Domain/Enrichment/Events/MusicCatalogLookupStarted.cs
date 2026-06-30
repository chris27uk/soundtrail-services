using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Domain.Enrichment.Events;

public sealed record MusicCatalogLookupStarted(
    MusicCatalogId MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset StartedAt) : IDomainEvent;
