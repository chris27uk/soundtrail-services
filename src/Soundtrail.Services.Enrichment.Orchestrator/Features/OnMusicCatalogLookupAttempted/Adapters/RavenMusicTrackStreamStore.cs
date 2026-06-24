using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Translators.MusicTrackEventStore;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Adapters;

public sealed class RavenMusicTrackStreamStore(
    IAsyncDocumentSession session,
    IMusicTrackStoredEventRecordTranslator translator) : IMusicTrackEventRepository
{
    public async Task<MusicTrackStream> LoadEventsAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var streamId = MusicTrackEventStreamMetadataRecordDto.GetDocumentId(musicCatalogId.Value);
        var metadata = await session.LoadAsync<MusicTrackEventStreamMetadataRecordDto>(streamId, cancellationToken);
        if (metadata is null)
        {
            return new MusicTrackStream(0, []);
        }

        var storedEvents = (await session.Advanced.LoadStartingWithAsync<MusicTrackStoredEventRecordDto>(
                $"music-track-events/{musicCatalogId.Value}/"))
            .OrderBy(x => x.Version)
            .ToList();

        return storedEvents.Count == 0
            ? new MusicTrackStream(0, [])
            : new MusicTrackStream(
                metadata.Version,
                storedEvents.Select(translator.ToDomainObject).ToArray());
    }

    public async Task<AppendMusicTrackStreamResult> AppendEventsAsync(
        MusicCatalogId musicCatalogId,
        int expectedVersion,
        CommandId commandId,
        IReadOnlyList<IMusicTrackEvent> events,
        CancellationToken cancellationToken)
    {
        var storedEventsToAppend = events.Select((@event, index) =>
                translator.ToDto(musicCatalogId, expectedVersion + index + 1, commandId, @event))
            .ToArray();

        session.Advanced.UseOptimisticConcurrency = true;
        var streamId = MusicTrackEventStreamMetadataRecordDto.GetDocumentId(musicCatalogId.Value);
        var metadata = await session.LoadAsync<MusicTrackEventStreamMetadataRecordDto>(streamId, cancellationToken)
            ?? new MusicTrackEventStreamMetadataRecordDto
            {
                Id = streamId,
                MusicCatalogId = musicCatalogId.Value
            };

        if (metadata.AppliedCommandIds.Contains(commandId.Value))
        {
            return new AppendMusicTrackStreamResult(false, metadata.Version, []);
        }

        if (metadata.Version != expectedVersion)
        {
            throw new MusicTrackStreamConcurrencyException(musicCatalogId, expectedVersion, metadata.Version);
        }

        metadata.AppliedCommandIds.Add(commandId.Value);
        metadata.Version += events.Count;
        metadata.UpdatedAtUtc = storedEventsToAppend.Length == 0
            ? DateTimeOffset.UtcNow
            : storedEventsToAppend.Max(x => x.OccurredAtUtc);

        await session.StoreAsync(metadata, cancellationToken);

        foreach (var storedEvent in storedEventsToAppend)
        {
            await session.StoreAsync(storedEvent, cancellationToken);
        }

        return new AppendMusicTrackStreamResult(true, metadata.Version, events.ToArray());
    }
}
