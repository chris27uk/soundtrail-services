using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Discovery.Messages;

public sealed record LookupStreamingLocationByIsrcMessage(
    MessageId Id,
    CorrelationId CorrelationId,
    DateTimeOffset CreatedAt,
    LookupPriorityBand Priority,
    TrackId TrackId,
    ProviderName Provider) : IMessage
{
    public DateTimeOffset RequestedAt => CreatedAt;
}
