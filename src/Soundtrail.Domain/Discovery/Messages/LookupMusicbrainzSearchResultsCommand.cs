using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Search.Contract;

namespace Soundtrail.Domain.Discovery.Commands;

public sealed record LookupMusicbrainzSearchResultsCommand(
    CommandId CommandId,
    CorrelationId CorrelationId,
    DateTimeOffset CreatedAt,
    LookupPriorityBand Priority,
    SearchCriteria SearchCriteria) : ICommand;
