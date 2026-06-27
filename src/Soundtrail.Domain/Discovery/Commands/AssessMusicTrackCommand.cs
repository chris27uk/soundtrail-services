using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery.Commands;

public sealed record AssessMusicTrackCommand(
    CommandId CommandId,
    CorrelationId CorrelationId,
    DateTimeOffset CreatedAt,
    LookupPriorityBand Priority,
    MusicCatalogId MusicCatalogId,
    MusicSearchCriteria? SearchTerm = null,
    int? TrustLevel = null,
    int? RiskScore = null) : ICommand
{
    public static CommandId Id(MusicCatalogId musicCatalogId, DateTimeOffset createdAt) =>
        CommandId.For($"AssessMusicTrack:{musicCatalogId.Value}:{createdAt.ToUnixTimeMilliseconds()}");
}
