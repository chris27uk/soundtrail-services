using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Domain.Events;

public sealed record DiscoveryCompleted(
    CatalogSearchCriteria Criteria,
    LookupPriorityBand Priority,
    bool WillBeLookedUp,
    string Reason,
    DateTimeOffset CompletedAt) : IDomainEvent;
