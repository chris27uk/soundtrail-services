using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Soundtrail.Adapters.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters;

public sealed class RavenCatalogSearchTrackingStore(
    IDocumentStore documentStore,
    IAsyncDocumentSession? session = null) : ICatalogSearchTrackingStore
{
    public async Task<CatalogSearchTracking?> FindByCriteriaAsync(
        LookupCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        var (activeSession, dispose) = OpenSession();
        using (dispose)
        {
            var document = await activeSession.LoadAsync<CatalogSearchStatusRecordDto>(
                CatalogSearchStatusRecordDto.GetDocumentId(DiscoveryQueryKey.StableValueFor(searchCriteria)),
                cancellationToken);

            return document is null || string.IsNullOrWhiteSpace(document.MusicCatalogId)
                ? null
                : new CatalogSearchTracking(
                    DiscoveryQueryKey.ToMusicSearchCriteria(document.Criteria),
                    MusicCatalogId.From(document.MusicCatalogId),
                    document.UpdatedAt);
        }
    }

    public async Task UpsertAsync(
        CatalogSearchTracking tracking,
        CancellationToken cancellationToken)
    {
        var ownsSession = session is null;
        var (activeSession, dispose) = OpenSession();
        using (dispose)
        {
            var persistentId = DiscoveryQueryKey.StableValueFor(tracking.SearchCriteria);
            var documentId = CatalogSearchStatusRecordDto.GetDocumentId(persistentId);
            var document = await activeSession.LoadAsync<CatalogSearchStatusRecordDto>(documentId, cancellationToken)
                ?? new CatalogSearchStatusRecordDto
                {
                    Id = documentId
                };

            document.Criteria = persistentId;
            document.MusicCatalogId = tracking.MusicCatalogId.Value;
            document.UpdatedAt = tracking.UpdatedAt;

            await activeSession.StoreAsync(document, cancellationToken);
            if (ownsSession)
            {
                await activeSession.SaveChangesAsync(cancellationToken);
            }
        }
    }

    public async Task<IReadOnlyList<CatalogSearchTracking>> GetByMusicCatalogIdAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var (activeSession, dispose) = OpenSession();
        using (dispose)
        {
            var documents = await activeSession
                .Query<CatalogSearchStatusRecordDto>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(document => document.MusicCatalogId == musicCatalogId.Value)
                .ToListAsync(cancellationToken);

            return documents
                .Where(document => !string.IsNullOrWhiteSpace(document.MusicCatalogId))
                .Select(document => new CatalogSearchTracking(
                    DiscoveryQueryKey.ToMusicSearchCriteria(document.Criteria),
                    MusicCatalogId.From(document.MusicCatalogId!),
                    document.UpdatedAt))
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
