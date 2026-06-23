using Soundtrail.Domain.Discovery;

namespace Soundtrail.Domain.Events;

public sealed record DiscoveryFailed(
    CatalogSearchCriteria Criteria,
    bool WillBeLookedUp,
    string Reason,
    DateTimeOffset FailedAt) : IDomainEvent;
