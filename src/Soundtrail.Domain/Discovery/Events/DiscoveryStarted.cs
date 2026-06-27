using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Events;

public sealed record DiscoveryStarted(
    MusicSearchCriteria SearchCriteria,
    LookupPriorityBand Priority,
    bool WillBeLookedUp,
    string Reason,
    DateTimeOffset StartedAt) : IDomainEvent;
