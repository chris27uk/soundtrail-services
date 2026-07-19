using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;
using Raven.Client.Documents.Changes;
using Raven.Client.Documents.Linq;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Internal.Projector.Features.OnLookupRecorded.Adapters;

internal sealed class LookupRecordedCdcService(
    IServiceScopeFactory scopeFactory,
    IDocumentStore documentStore) : BackgroundService
{
    private const string CheckpointId = "projector/checkpoints/lookup-recorded";
    private readonly SemaphoreSlim gate = new(1, 1);
    private IDisposable? subscription;
    private IDatabaseChanges? changes;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        changes = documentStore.Changes();
        await changes.EnsureConnectedNow();

        await CatchUpAsync(stoppingToken);

        subscription = changes
            .ForDocumentsInCollection(nameof(RavenStoredEventRecord))
            .Subscribe(new DocumentChangeObserver(change =>
            {
                if (change.Type.HasFlag(DocumentChangeTypes.Put))
                {
                    _ = CatchUpAsync(CancellationToken.None);
                }
            }));

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    public override void Dispose()
    {
        subscription?.Dispose();
        gate.Dispose();
        base.Dispose();
    }

    private async Task CatchUpAsync(CancellationToken cancellationToken)
    {
        if (!await gate.WaitAsync(TimeSpan.Zero, cancellationToken))
        {
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var discoveryOutcomeHandler = scope.ServiceProvider.GetRequiredService<DiscoveryOutcomeProjectorHandler>();
            var streamingLocationHandler = scope.ServiceProvider.GetRequiredService<StreamingLocationDiscoveredProjectorHandler>();

            using var session = documentStore.OpenAsyncSession();
            var checkpoint = await session.LoadAsync<ProjectorCheckpointRecordDto>(CheckpointId, cancellationToken)
                ?? new ProjectorCheckpointRecordDto { Id = CheckpointId };

            var query = session.Query<RavenStoredEventRecord>()
                .Where(x => x.AggregateType == "catalog-search"
                    && (x.EventType == "artist-discovered"
                        || x.EventType == "album-discovered"
                        || x.EventType == "track-discovered"
                        || x.EventType == "streaming-location-discovered"
                        || x.EventType == "playlist-tracks-discovered"));

            if (checkpoint.LastOccurredAtUtc is { } lastOccurredAtUtc)
            {
                query = query.Where(x =>
                    x.OccurredAtUtc > lastOccurredAtUtc
                    || (x.OccurredAtUtc == lastOccurredAtUtc && string.CompareOrdinal(x.EventId, checkpoint.LastEventId!) > 0));
            }

            var events = await query
                .OrderBy(x => x.OccurredAtUtc)
                .ThenBy(x => x.EventId)
                .Take(128)
                .ToListAsync(cancellationToken);

            if (events.Count == 0)
            {
                return;
            }

            foreach (var storedEvent in events)
            {
                switch (storedEvent.EventType)
                {
                    case "artist-discovered":
                        var artistDiscovered = TypeTranslationRegistry.Default.ToDomainObject<ArtistDiscovered>(
                            storedEvent.Body ?? throw new InvalidOperationException("ArtistDiscovered events must include a body."));
                        await discoveryOutcomeHandler.Handle(artistDiscovered, cancellationToken);
                        break;
                    case "album-discovered":
                        var albumDiscovered = TypeTranslationRegistry.Default.ToDomainObject<AlbumDiscovered>(
                            storedEvent.Body ?? throw new InvalidOperationException("AlbumDiscovered events must include a body."));
                        await discoveryOutcomeHandler.Handle(albumDiscovered, cancellationToken);
                        break;
                    case "track-discovered":
                        var trackDiscovered = TypeTranslationRegistry.Default.ToDomainObject<TrackDiscovered>(
                            storedEvent.Body ?? throw new InvalidOperationException("TrackDiscovered events must include a body."));
                        await discoveryOutcomeHandler.Handle(trackDiscovered, cancellationToken);
                        break;
                    case "streaming-location-discovered":
                        var streamingLocationDiscovered = TypeTranslationRegistry.Default.ToDomainObject<StreamingLocationDiscovered>(
                            storedEvent.Body ?? throw new InvalidOperationException("StreamingLocationDiscovered events must include a body."));
                        await streamingLocationHandler.Handle(streamingLocationDiscovered, cancellationToken);
                        break;
                    case "playlist-tracks-discovered":
                        var playlistTracksDiscovered = TypeTranslationRegistry.Default.ToDomainObject<PlaylistTracksDiscovered>(
                            storedEvent.Body ?? throw new InvalidOperationException("PlaylistTracksDiscovered events must include a body."));
                        await discoveryOutcomeHandler.Handle(playlistTracksDiscovered, cancellationToken);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported lookup recorded event type '{storedEvent.EventType}'.");
                }

                checkpoint.LastOccurredAtUtc = storedEvent.OccurredAtUtc;
                checkpoint.LastEventId = storedEvent.EventId;
            }

            await session.StoreAsync(checkpoint, cancellationToken);
            await session.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    private sealed class DocumentChangeObserver(Action<DocumentChange> onNext) : IObserver<DocumentChange>
    {
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(DocumentChange value) => onNext(value);
    }
}
