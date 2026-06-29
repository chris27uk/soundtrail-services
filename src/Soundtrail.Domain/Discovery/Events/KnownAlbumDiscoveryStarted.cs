using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record KnownAlbumDiscoveryStarted(
    ArtistId ArtistId,
    AlbumId AlbumId,
    LookupPriorityBand Priority,
    string Reason,
    DateTimeOffset StartedAt) : IDomainEvent;
