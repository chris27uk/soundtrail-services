using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Model;
using Soundtrail.Translators.MusicTrackEventStore;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.Adapters;

public sealed class RavenLoadMusicTrackEventsForCatalogReplay(
    IAsyncDocumentSession session,
    IMusicTrackStoredEventRecordTranslator translator) : ILoadMusicTrackEventsForCatalogReplayPort
{
    public async Task<IReadOnlyList<VersionedMusicTrackEvent>> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var events = (await session.Advanced.LoadStartingWithAsync<MusicTrackStoredEventRecordDto>(
                $"music-track-events/{musicCatalogId.Value}/"))
            .OrderBy(x => x.Version)
            .Select(x => new VersionedMusicTrackEvent(x.Version, translator.ToDomainObject(x)))
            .ToArray();

        return events;
    }
}
