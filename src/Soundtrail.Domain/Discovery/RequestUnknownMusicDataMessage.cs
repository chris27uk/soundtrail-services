using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery;

public sealed record RequestUnknownMusicDataMessage : IPrioritisedMessage
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

    int? IPrioritisedMessage.TrustLevel => TrustLevel;

    int? IPrioritisedMessage.RiskScore => RiskScore;
}
