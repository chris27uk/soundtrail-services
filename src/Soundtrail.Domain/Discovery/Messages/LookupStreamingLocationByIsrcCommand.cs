using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Discovery.Messages;

public sealed record LookupStreamingLocationByIsrcCommand(
    CommandId CommandId,
    CorrelationId CorrelationId,
    DateTimeOffset CreatedAt,
    LookupPriorityBand Priority,
    TrackId TrackId,
    ProviderName Provider) : ICommand
{
    public DateTimeOffset RequestedAt => CreatedAt;
}
