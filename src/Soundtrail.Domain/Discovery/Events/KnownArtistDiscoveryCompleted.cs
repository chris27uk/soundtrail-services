using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record KnownArtistDiscoveryCompleted(
    ArtistId ArtistId,
    LookupPriorityBand Priority,
    LookupSource SourceProvider,
    string Reason,
    DateTimeOffset CompletedAt,
    string ArtistName,
    string? SourceArtistId) : IDomainEvent;
