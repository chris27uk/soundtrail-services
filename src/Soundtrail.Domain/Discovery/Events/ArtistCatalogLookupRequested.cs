using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record ArtistCatalogLookupRequested(
    ArtistId ArtistId,
    DateTimeOffset RequestedAt,
    CorrelationId CorrelationId) : IDomainEvent;
