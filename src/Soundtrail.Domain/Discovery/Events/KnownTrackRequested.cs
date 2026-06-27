using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record KnownTrackRequested(
    TrackId TrackId,
    PlaybackProviderFilter Playback,
    DateTimeOffset RequestedAt,
    CorrelationId CorrelationId) : IDomainEvent;
