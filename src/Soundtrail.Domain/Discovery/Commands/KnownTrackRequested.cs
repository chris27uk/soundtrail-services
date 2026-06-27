using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery.Commands;

public sealed record KnownTrackRequested(
    TrackId TrackId,
    PlaybackProviderFilter Playback,
    DateTimeOffset OccurredAt,
    CorrelationId CorrelationId) : ICommand
{
    public CommandId CommandId { get; init; } = CommandId.New();

    public DateTimeOffset CreatedAt => OccurredAt;

    public LookupPriorityBand Priority => LookupPriorityBand.High;
}
