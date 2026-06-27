using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery.Commands;

public sealed record KnownTrackRequested(
    TrackId TrackId,
    PlaybackProviderFilter Playback,
    DateTimeOffset OccurredAt,
    CorrelationId CorrelationId);
