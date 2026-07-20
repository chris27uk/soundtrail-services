using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery;

public sealed record RequestUnknownMusicDataMessage : IMessage
{
    public RequestUnknownMusicDataMessage(SearchCriteria SearchCriteria,
        LookupPriorityBand Priority,
        int TrustLevel,
        int RiskScore,
        DateTimeOffset RequestedAt,
        MessageId? CommandId = null,
        CorrelationId? CorrelationId = null)
    {
        this.SearchCriteria = SearchCriteria;
        this.Priority = Priority;
        this.TrustLevel = TrustLevel;
        this.RiskScore = RiskScore;
        this.RequestedAt = RequestedAt;
        this.Id = CommandId ?? Soundtrail.Domain.Common.MessageId.New();
        this.CorrelationId = CorrelationId ?? Soundtrail.Domain.Common.CorrelationId.New();
    }

    public SearchCriteria SearchCriteria { get; init; }
    public LookupPriorityBand Priority { get; init; }
    public int TrustLevel { get; init; }
    public int RiskScore { get; init; }
    public DateTimeOffset RequestedAt { get; init; }
    public MessageId Id { get; init; }
    public CorrelationId CorrelationId { get; init; }

    public void Deconstruct(out SearchCriteria SearchCriteria, out LookupPriorityBand Priority, out int TrustLevel, out int RiskScore, out DateTimeOffset RequestedAt, out MessageId messageId, out CorrelationId CorrelationId)
    {
        SearchCriteria = this.SearchCriteria;
        Priority = this.Priority;
        TrustLevel = this.TrustLevel;
        RiskScore = this.RiskScore;
        RequestedAt = this.RequestedAt;
        messageId = this.Id;
        CorrelationId = this.CorrelationId;
    }
}
