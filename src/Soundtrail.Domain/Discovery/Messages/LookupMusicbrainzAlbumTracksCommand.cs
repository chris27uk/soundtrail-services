using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Discovery.Messages;

public sealed record LookupMusicbrainzAlbumTracksCommand(
    CommandId CommandId,
    CorrelationId CorrelationId,
    DateTimeOffset CreatedAt,
    LookupPriorityBand Priority,
    AlbumId AlbumId) : ICommand
{
    public DateTimeOffset RequestedAt => CreatedAt;
}
