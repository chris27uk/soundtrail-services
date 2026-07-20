using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Discovery.Messages;

public sealed record LookupPlaylistTracksByProviderMessage(
    MessageId Id,
    CorrelationId CorrelationId,
    DateTimeOffset CreatedAt,
    LookupPriorityBand Priority,
    PlaylistId PlaylistId,
    ProviderName Provider) : IMessage
{
    public DateTimeOffset RequestedAt => CreatedAt;
}
