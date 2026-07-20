using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Discovery.Messages;

public sealed record LookupMusicbrainzAlbumTracksMessage(
    MessageId Id,
    CorrelationId CorrelationId,
    DateTimeOffset CreatedAt,
    LookupPriorityBand Priority,
    AlbumId AlbumId) : IMessage
{
    public DateTimeOffset RequestedAt => CreatedAt;
}
