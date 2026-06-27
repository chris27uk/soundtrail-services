using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record DiscoveryStarted(
    MusicSearchCriteria SearchCriteria,
    LookupPriorityBand Priority,
    bool WillBeLookedUp,
    string Reason,
    DateTimeOffset StartedAt) : IDomainEvent;
