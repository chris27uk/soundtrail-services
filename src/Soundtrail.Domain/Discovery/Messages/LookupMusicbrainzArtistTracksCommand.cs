using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Discovery.Commands;

public sealed record LookupMusicbrainzArtistTracksCommand(
    CommandId CommandId,
    CorrelationId CorrelationId,
    DateTimeOffset CreatedAt,
    LookupPriorityBand Priority,
    ArtistId ArtistId) : ICommand;
