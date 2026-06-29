using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record KnownArtistDiscoveryFailed(
    ArtistId ArtistId,
    LookupPriorityBand Priority,
    string Reason,
    DateTimeOffset FailedAt) : IDomainEvent;
