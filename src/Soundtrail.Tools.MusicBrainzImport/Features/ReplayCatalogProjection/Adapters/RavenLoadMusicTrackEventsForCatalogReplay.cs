using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Projection;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.EventStore;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.Adapters;

public sealed class RavenLoadMusicTrackEventsForCatalogReplay(
    IAsyncDocumentSession session,
    ITypeRegistry translator) : ILoadMusicTrackEventsForCatalogReplayPort
{
    public async Task<IReadOnlyList<VersionedMusicTrackEvent>> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var events = (await session.Advanced.LoadStartingWithAsync<RavenStoredEventRecord>(
                $"music-track-events/{musicCatalogId.Value}/"))
            .OrderBy(x => x.Version)
            .Select(x => new VersionedMusicTrackEvent(
                x.Version,
                translator.ToDomainObject<IMusicTrackEvent>(
                    x.Body ?? throw new InvalidOperationException($"Stored event '{x.Id}' is missing a body."))))
            .ToArray();

        return events;
    }
}
