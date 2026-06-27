using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record AlbumCatalogLookupRequested(
    ArtistId? ArtistId,
    AlbumId AlbumId,
    DateTimeOffset RequestedAt,
    CorrelationId CorrelationId) : IDomainEvent;
