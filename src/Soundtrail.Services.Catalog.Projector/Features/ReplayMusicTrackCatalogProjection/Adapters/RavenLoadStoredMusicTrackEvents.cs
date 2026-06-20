using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog.Adapters;
using Soundtrail.Services.Catalog.Projector.Features.ReplayMusicTrackCatalogProjection.StoredEvents;

namespace Soundtrail.Services.Catalog.Projector.Features.ReplayMusicTrackCatalogProjection.Adapters;

public sealed class RavenLoadStoredMusicTrackEvents(
    IAsyncDocumentSession session) : ILoadStoredMusicTrackEventsPort
{
    public async Task<IReadOnlyList<VersionedMusicTrackEvent>> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var events = await session.Advanced.AsyncDocumentQuery<MusicTrackStoredEventRecordDto>()
            .WhereEquals(nameof(MusicTrackStoredEventRecordDto.MusicCatalogId), musicCatalogId.Value)
            .ToListAsync(cancellationToken);

        return events
            .OrderBy(x => x.Version)
            .Select(item => new VersionedMusicTrackEvent(item.Version, item.ToDomainEvent()))
            .ToArray();
    }
}
