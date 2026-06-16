using Soundtrail.Domain.Discovery;

namespace Soundtrail.Domain.Events;

public sealed record DiscoveryRejected(
    CatalogSearchCriteria Criteria,
    bool WillBeLookedUp,
    string Reason,
    DateTimeOffset RejectedAt) : IDomainEvent;
