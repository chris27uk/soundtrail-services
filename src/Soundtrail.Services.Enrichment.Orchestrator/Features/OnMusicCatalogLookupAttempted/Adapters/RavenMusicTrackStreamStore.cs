using Raven.Client.Documents.Session;
using Soundtrail.Adapters.EventSourcing;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Projection;
using Soundtrail.Adapters.MusicTrackEventStore;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Adapters;

public sealed class RavenMusicTrackStreamStore(
    IAsyncDocumentSession session,
    IMusicTrackStoredEventRecordTranslator translator) : IMusicTrackEventRepository
{
    private readonly RavenEventStore<MusicCatalogId, IMusicTrackEvent, MusicTrackStoredEventRecordDto, MusicTrackEventStreamMetadataRecordDto> eventStore =
        new(
            session,
            streamId => MusicTrackEventStreamMetadataRecordDto.GetDocumentId(streamId.StableValue),
            (streamId, metadataId) => new MusicTrackEventStreamMetadataRecordDto
            {
                Id = metadataId,
                MusicCatalogId = streamId.StableValue
            },
            streamId => $"music-track-events/{streamId.StableValue}/",
            (streamId, version, operationId, @event) =>
                translator.ToDto(streamId, version, CommandId.From(operationId?.StableValue ?? string.Empty), @event),
            translator.ToDomainObject,
            storedEvent => storedEvent.OccurredAtUtc,
            storedEvent => storedEvent.Version);

    public async Task<MusicTrackStream> LoadEventsAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var stream = await eventStore.LoadAsync(musicCatalogId, cancellationToken);
        return new MusicTrackStream(stream.Version, stream.Events);
    }

    public async Task<AppendMusicTrackStreamResult> AppendEventsAsync(
        MusicCatalogId musicCatalogId,
        int expectedVersion,
        CommandId commandId,
        IReadOnlyList<IMusicTrackEvent> events,
        CancellationToken cancellationToken)
    {
        var append = await eventStore.AppendAsync(
            new AppendRequest<MusicCatalogId, IMusicTrackEvent>(
                musicCatalogId,
                expectedVersion,
                events,
                OperationId.From(commandId.Value)),
            cancellationToken);

        return append.Outcome switch
        {
            AppendOutcome.VersionMismatch => throw new MusicTrackStreamConcurrencyException(musicCatalogId, expectedVersion, append.Version),
            AppendOutcome.DuplicateOperation => new AppendMusicTrackStreamResult(false, append.Version, []),
            _ => new AppendMusicTrackStreamResult(true, append.Version, append.Events)
        };
    }
}
