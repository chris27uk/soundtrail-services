using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Events;

public sealed record DiscoveryRejected(
    MusicSearchCriteria SearchCriteria,
    bool WillBeLookedUp,
    string Reason,
    DateTimeOffset RejectedAt) : IDomainEvent;
