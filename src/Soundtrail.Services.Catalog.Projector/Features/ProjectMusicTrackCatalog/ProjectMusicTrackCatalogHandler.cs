using Raven.Client.Documents;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog.Adapters;

namespace Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog;

public sealed class ProjectMusicTrackCatalogHandler(
    IDocumentStore documentStore,
    CatalogMusicTrackProjectionApplier projectionApplier)
{
    public async Task HandleAsync(
        IReadOnlyCollection<MusicTrackStoredEventRecordDto> storedEvents,
        CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        session.Advanced.WaitForIndexesAfterSaveChanges();

        foreach (var storedEvent in storedEvents
                     .OrderBy(item => item.MusicCatalogId, StringComparer.Ordinal)
                     .ThenBy(item => item.Version))
        {
            await projectionApplier.ApplyStoredEventAsync(storedEvent, session, cancellationToken);
        }

        await session.SaveChangesAsync(cancellationToken);
    }
}
