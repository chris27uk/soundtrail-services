using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record DiscoveryCompleted(
    MusicSearchCriteria SearchCriteria,
    LookupPriorityBand Priority,
    bool WillBeLookedUp,
    string Reason,
    DateTimeOffset CompletedAt) : IDomainEvent;
