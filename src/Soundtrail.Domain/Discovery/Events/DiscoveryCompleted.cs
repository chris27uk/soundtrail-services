using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Events;

public sealed record DiscoveryCompleted(
    MusicSearchCriteria SearchCriteria,
    LookupPriorityBand Priority,
    bool WillBeLookedUp,
    string Reason,
    DateTimeOffset CompletedAt) : IDomainEvent;
