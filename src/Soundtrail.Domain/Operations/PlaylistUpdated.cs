using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Operations;

public sealed record PlaylistUpdated(string Name, TrackId[] Tracks) : ICommand
{
    public CommandId CommandId { get; init; } = CommandId.New();

    public CorrelationId CorrelationId { get; init; } = CorrelationId.New();

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
