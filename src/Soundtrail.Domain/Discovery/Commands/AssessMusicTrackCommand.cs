using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;

namespace Soundtrail.Domain.Discovery;

public sealed record AssessMusicTrackCommand(
    CommandId CommandId,
    CorrelationId CorrelationId,
    DateTimeOffset CreatedAt,
    LookupPriorityBand Priority,
    MusicCatalogId MusicCatalogId,
    CatalogSearchCriteria? Criteria = null,
    int? TrustLevel = null,
    int? RiskScore = null) : ICommand
{
    public static CommandId Id(MusicCatalogId musicCatalogId, DateTimeOffset createdAt) =>
        CommandId.For($"AssessMusicTrack:{musicCatalogId.Value}:{createdAt.ToUnixTimeMilliseconds()}");
}
