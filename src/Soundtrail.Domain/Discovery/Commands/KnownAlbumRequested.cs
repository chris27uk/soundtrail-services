using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Discovery.Commands;

public sealed record KnownAlbumRequested(
    ArtistId ArtistId,
    AlbumId AlbumId,
    DateTimeOffset OccurredAt,
    CorrelationId CorrelationId) : ICommand
{
    public CommandId CommandId { get; init; } = CommandId.New();

    public DateTimeOffset CreatedAt => OccurredAt;

    public LookupPriorityBand Priority => LookupPriorityBand.High;
}
