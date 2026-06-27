using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record DiscoveryRejected(
    MusicSearchCriteria SearchCriteria,
    bool WillBeLookedUp,
    string Reason,
    DateTimeOffset RejectedAt) : IDomainEvent;
