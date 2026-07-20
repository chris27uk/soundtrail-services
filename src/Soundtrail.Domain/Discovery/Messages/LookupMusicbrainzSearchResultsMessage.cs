using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery.Messages;

public sealed record LookupMusicbrainzSearchResultsMessage(
    MessageId Id,
    CorrelationId CorrelationId,
    DateTimeOffset CreatedAt,
    LookupPriorityBand Priority,
    SearchCriteria SearchCriteria) : IMessage
{
    public DateTimeOffset RequestedAt => CreatedAt;
}
