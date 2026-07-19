using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery.Messages;

public sealed record LookupMusicbrainzSearchResultsCommand(
    CommandId CommandId,
    CorrelationId CorrelationId,
    DateTimeOffset CreatedAt,
    LookupPriorityBand Priority,
    SearchCriteria SearchCriteria) : ICommand
{
    public DateTimeOffset RequestedAt => CreatedAt;
}
