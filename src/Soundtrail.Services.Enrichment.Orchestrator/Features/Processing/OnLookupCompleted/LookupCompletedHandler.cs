using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupCompleted.Collaborators;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupCompleted;

public sealed class LookupCompletedHandler(IEventStreamRepository<CatalogWorkId> repository) : IHandler<CatalogLookupCompleted>
{
    public async Task Handle(CatalogLookupCompleted request, CancellationToken cancellationToken = default)
    {
        var result = request.Result;
        var resultContext = ResultContext(result);
        var context = await LookupCompletionContextReader.ReadAsync(repository, resultContext.StreamId, cancellationToken);
        var historyContext = new DiscoveryHistory.SearchRequestContext(
            resultContext.OriginalCommandId,
            0,
            0,
            CompletedAt(result),
            context.CorrelationId);

        var loaded = await DiscoveryHistory.LoadAsync(repository, resultContext.StreamId, historyContext, cancellationToken);
        var history = loaded.Aggregate;

        result.Match(
            succeeded =>
            {
                LookupCompletedEventRecorder.RecordSuccess(history, succeeded, context);
                history.Complete(context.Target, context.Priority, "Lookup completed.", succeeded.CompletedAt);
                return 0;
            },
            duplicate =>
            {
                history.Complete(context.Target, context.Priority, duplicate.Reason, duplicate.CompletedAt);
                return 0;
            },
            notFound =>
            {
                history.FailAttempt(context.Target, notFound.Reason, notFound.CompletedAt);
                return 0;
            },
            deferred =>
            {
                history.DeferResult(context.Target, context.Priority, deferred.DeferredUntil, deferred.Reason, deferred.CompletedAt);
                return 0;
            },
            failed =>
            {
                history.FailAttempt(context.Target, failed.Reason, failed.CompletedAt);
                return 0;
            });

        await history.SaveAsync(cancellationToken);
    }

    private static DateTimeOffset CompletedAt(Domain.Discovery.LookupResult result) =>
        result.Match(
            succeeded => succeeded.CompletedAt,
            duplicate => duplicate.CompletedAt,
            notFound => notFound.CompletedAt,
            deferred => deferred.CompletedAt,
            failed => failed.CompletedAt);

    private static Domain.Discovery.LookupResultContext ResultContext(Domain.Discovery.LookupResult result) =>
        result.Match(
            succeeded => succeeded.Context,
            duplicate => duplicate.Context,
            notFound => notFound.Context,
            deferred => deferred.Context,
            failed => failed.Context);
}
