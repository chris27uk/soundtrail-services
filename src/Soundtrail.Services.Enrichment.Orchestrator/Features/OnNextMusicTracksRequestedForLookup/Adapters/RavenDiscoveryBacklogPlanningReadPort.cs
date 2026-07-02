using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Soundtrail.Adapters.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnNextMusicTracksRequestedForLookup.Adapters;

public sealed class RavenDiscoveryBacklogPlanningReadPort(
    IDocumentStore documentStore,
    IAsyncDocumentSession? session = null) : IDiscoveryBacklogPlanningReadPort
{
    public async Task<IReadOnlyList<DiscoveryBacklogCandidate>> GetPlanningCandidatesAsync(
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken)
    {
        var (activeSession, dispose) = OpenSession();
        using (dispose)
        {
            var deferredStatus = CatalogSearchLifecycleStatus.Deferred.ToString();
            var requestedStatus = CatalogSearchLifecycleStatus.Requested.ToString();

            var documents = await activeSession
                .Query<CatalogSearchStatusRecordDto>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(document =>
                    document.MusicCatalogId != null
                    && document.WillBeLookedUp
                    && (document.Status == requestedStatus
                        || (document.Status == deferredStatus && document.EarliestExpectedCompletionAt <= now)))
                .OrderBy(document => document.EarliestExpectedCompletionAt)
                .ThenBy(document => document.UpdatedAt)
                .Take(take)
                .ToListAsync(cancellationToken);

            return documents
                .Where(document => !string.IsNullOrWhiteSpace(document.MusicCatalogId))
                .Select(document => new DiscoveryBacklogCandidate(
                    DiscoveryQueryKey.ToMusicSearchCriteria(document.Criteria),
                    MusicCatalogId.From(document.MusicCatalogId!),
                    document.UpdatedAt,
                    document.EarliestExpectedCompletionAt))
                .ToArray();
        }
    }

    private (IAsyncDocumentSession Session, IDisposable Dispose) OpenSession()
    {
        if (session is not null)
        {
            return (session, NoopDisposable.Instance);
        }

        var openedSession = documentStore.OpenAsyncSession();
        return (openedSession, openedSession);
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
