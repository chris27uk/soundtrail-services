using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Projection;
using Soundtrail.Services.Internal.Projector.Features.OnReplayMusicTrack.StoredEvents;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Services.Internal.Projector.Features.OnReplayMusicTrack.Adapters;

public sealed class RavenLoadStoredMusicTrackEvents(
    IAsyncDocumentSession session,
    ITypeRegistry translator) : ILoadStoredMusicTrackEventsPort
{
    public async Task<IReadOnlyList<VersionedMusicTrackEvent>> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var events = (await session.Advanced.LoadStartingWithAsync<RavenStoredEventRecord>(
                $"music-track-events/{musicCatalogId.Value}/"))
            .ToList();

        return events
            .OrderBy(x => x.Version)
            .Select(item => new VersionedMusicTrackEvent(
                item.Version,
                translator.ToDomainObject<IMusicTrackEvent>(
                    item.Body ?? throw new InvalidOperationException($"Stored event '{item.Id}' is missing a body."))))
            .ToArray();
    }
}
