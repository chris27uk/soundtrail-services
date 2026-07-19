using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Discovery.Commands;

public sealed record LookupPlaylistTracksByProviderCommand(
    CommandId CommandId,
    CorrelationId CorrelationId,
    DateTimeOffset CreatedAt,
    LookupPriorityBand Priority,
    PlaylistId PlaylistId,
    ProviderName Provider) : ICommand;
