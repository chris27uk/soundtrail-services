using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;

public sealed class RavenMusicTrackStreamStore(
    IAsyncDocumentSession session) : IMusicTrackEventRepository
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

        var storedEvents = await session.Advanced.AsyncDocumentQuery<MusicTrackStoredEventRecordDto>()
            .WhereEquals(nameof(MusicTrackStoredEventRecordDto.MusicCatalogId), musicCatalogId.Value)
            .OrderBy(nameof(MusicTrackStoredEventRecordDto.Version))
            .ToListAsync(cancellationToken);

        return storedEvents.Count == 0
            ? new MusicTrackStream(0, [])
            : storedEvents.ToDomain(metadata.Version);
    }

    public async Task<AppendMusicTrackStreamResult> AppendEventsAsync(
        MusicCatalogId musicCatalogId,
        int expectedVersion,
        CommandId commandId,
        IReadOnlyList<IMusicTrackEvent> events,
        CancellationToken cancellationToken)
    {
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
        metadata.UpdatedAtUtc = events.Count == 0
            ? DateTimeOffset.UtcNow
            : events.Max(x => x.OccurredAtUtc());

        await session.StoreAsync(metadata, cancellationToken);

        foreach (var storedEvent in events.ToStoredEventRecordDtos(musicCatalogId, expectedVersion, commandId))
        {
            await session.StoreAsync(storedEvent, cancellationToken);
        }

        return new AppendMusicTrackStreamResult(true, metadata.Version, events.ToArray());
    }
}
