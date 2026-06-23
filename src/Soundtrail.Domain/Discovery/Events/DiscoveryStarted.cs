using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Domain.Events;

public sealed record DiscoveryStarted(
    CatalogSearchCriteria Criteria,
    LookupPriorityBand Priority,
    bool WillBeLookedUp,
    string Reason,
    DateTimeOffset StartedAt) : IDomainEvent;
