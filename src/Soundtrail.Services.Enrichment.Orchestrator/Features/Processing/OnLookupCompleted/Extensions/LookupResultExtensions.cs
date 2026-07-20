using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupCompleted.Extensions;

internal static class LookupResultExtensions
{
    public static DiscoveryHistory.SearchRequestContext ToAggregateContext(this CatalogLookupCompleted completed)
    {
        return new DiscoveryHistory.SearchRequestContext(
            completed.Result.ToResultContext().OriginalCommandId,
            0,
            0,
            completed.RequestedAt,
            completed.CorrelationId);
    }

    public static CatalogWorkId StreamId(this LookupResult result) => result.ToResultContext().StreamId;

    private static DateTimeOffset CompletedAt(this LookupResult result) =>
        result.Match(
            succeeded => succeeded.CompletedAt,
            duplicate => duplicate.CompletedAt,
            notFound => notFound.CompletedAt,
            deferred => deferred.CompletedAt,
            failed => failed.CompletedAt);

    private static LookupResultContext ToResultContext(this LookupResult result) =>
        result.Match(
            succeeded => succeeded.Context,
            duplicate => duplicate.Context,
            notFound => notFound.Context,
            deferred => deferred.Context,
            failed => failed.Context);
}
