using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Discovery.Messages;

public sealed record LookupPlaylistTracksByProviderCommand(
    CommandId CommandId,
    CorrelationId CorrelationId,
    DateTimeOffset CreatedAt,
    LookupPriorityBand Priority,
    PlaylistId PlaylistId,
    ProviderName Provider) : ICommand
{
    public DateTimeOffset RequestedAt => CreatedAt;
}
