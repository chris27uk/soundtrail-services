using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;
using Soundtrail.Translators.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters;

public sealed class RavenCatalogSearchTrackingStore(
    IDocumentStore documentStore,
    IAsyncDocumentSession? session = null) : ICatalogSearchTrackingStore
{
    public async Task<CatalogSearchTracking?> FindByCriteriaAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        var (activeSession, dispose) = OpenSession();
        using (dispose)
        {
            var document = await activeSession.LoadAsync<RavenCatalogSearchTrackingRecordDto>(
                RavenCatalogSearchTrackingRecordDto.GetDocumentId(MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria)),
                cancellationToken);

            return document is null
                ? null
                : new CatalogSearchTracking(
                    MusicSearchTermPersistentIdTranslator.ToDomainObject(document.Criteria),
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
            var persistentId = MusicSearchTermPersistentIdTranslator.ToPersistentId(tracking.SearchCriteria);
            var documentId = RavenCatalogSearchTrackingRecordDto.GetDocumentId(persistentId);
            var document = await activeSession.LoadAsync<RavenCatalogSearchTrackingRecordDto>(documentId, cancellationToken)
                ?? new RavenCatalogSearchTrackingRecordDto
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
                .Query<RavenCatalogSearchTrackingRecordDto>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(document => document.MusicCatalogId == musicCatalogId.Value)
                .ToListAsync(cancellationToken);

            return documents
                .Select(document => new CatalogSearchTracking(
                    MusicSearchTermPersistentIdTranslator.ToDomainObject(document.Criteria),
                    MusicCatalogId.From(document.MusicCatalogId),
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
