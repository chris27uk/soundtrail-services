using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record DiscoveryFailed(
    MusicSearchCriteria SearchCriteria,
    bool WillBeLookedUp,
    string Reason,
    DateTimeOffset FailedAt) : IDomainEvent;
