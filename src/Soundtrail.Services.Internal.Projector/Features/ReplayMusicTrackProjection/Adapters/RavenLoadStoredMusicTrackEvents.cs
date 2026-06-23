using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Internal.Projector.Features.ProjectMusicTrackProjection.Adapters;
using Soundtrail.Services.Internal.Projector.Features.ReplayMusicTrackProjection.StoredEvents;

namespace Soundtrail.Services.Internal.Projector.Features.ReplayMusicTrackProjection.Adapters;

public sealed class RavenLoadStoredMusicTrackEvents(
    IAsyncDocumentSession session) : ILoadStoredMusicTrackEventsPort
{
    public async Task<IReadOnlyList<VersionedMusicTrackEvent>> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var events = (await session.Advanced.LoadStartingWithAsync<MusicTrackStoredEventRecordDto>(
                $"music-track-events/{musicCatalogId.Value}/"))
            .ToList();

        return events
            .OrderBy(x => x.Version)
            .Select(item => new VersionedMusicTrackEvent(item.Version, item.ToDomainEvent()))
            .ToArray();
    }
}
