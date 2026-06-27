using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery.Commands;

public sealed record SearchCatalogRequested(
    MusicSearchCriteria SearchCriteria,
    PlaybackProviderFilter Playback,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset OccurredAt,
    CorrelationId CorrelationId) : ICommand
{
    public CommandId CommandId { get; init; } = CommandId.New();

    public DateTimeOffset CreatedAt => OccurredAt;

    public LookupPriorityBand Priority => LookupPriorityBand.High;
}
