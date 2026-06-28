using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Support;

public sealed record RecordCatalogSearchCandidateCommand(
    MusicSearchCriteria SearchCriteria,
    MusicCatalogId MusicCatalogId,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset OccurredAt,
    CorrelationId CorrelationId) : ICommand
{
    public CommandId CommandId { get; init; } = CommandId.New();

    public DateTimeOffset CreatedAt => OccurredAt;

    public LookupPriorityBand Priority => LookupPriorityBand.Low;
}
