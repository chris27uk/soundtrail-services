using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Events;

public sealed record DiscoveryFailed(
    MusicSearchCriteria SearchCriteria,
    bool WillBeLookedUp,
    string Reason,
    DateTimeOffset FailedAt) : IDomainEvent;
