using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery;

public sealed record RequestUnknownMusicDataCommand : ICommand
{
    public RequestUnknownMusicDataCommand(SearchCriteria SearchCriteria,
        LookupPriorityBand Priority,
        int TrustLevel,
        int RiskScore,
        DateTimeOffset RequestedAt,
        CommandId? CommandId = null,
        CorrelationId? CorrelationId = null)
    {
        this.SearchCriteria = SearchCriteria;
        this.Priority = Priority;
        this.TrustLevel = TrustLevel;
        this.RiskScore = RiskScore;
        this.RequestedAt = RequestedAt;
        this.CommandId = CommandId ?? Soundtrail.Domain.Common.CommandId.New();
        this.CorrelationId = CorrelationId ?? Soundtrail.Domain.Common.CorrelationId.New();
    }

    public SearchCriteria SearchCriteria { get; init; }
    public LookupPriorityBand Priority { get; init; }
    public int TrustLevel { get; init; }
    public int RiskScore { get; init; }
    public DateTimeOffset RequestedAt { get; init; }
    public CommandId CommandId { get; init; }
    public CorrelationId CorrelationId { get; init; }

    public void Deconstruct(out SearchCriteria SearchCriteria, out LookupPriorityBand Priority, out int TrustLevel, out int RiskScore, out DateTimeOffset RequestedAt, out CommandId CommandId, out CorrelationId CorrelationId)
    {
        SearchCriteria = this.SearchCriteria;
        Priority = this.Priority;
        TrustLevel = this.TrustLevel;
        RiskScore = this.RiskScore;
        RequestedAt = this.RequestedAt;
        CommandId = this.CommandId;
        CorrelationId = this.CorrelationId;
    }
}
