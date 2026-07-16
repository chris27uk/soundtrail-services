using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Contracts.Persistence;

namespace Soundtrail.Services.Projector.Features.OnMusicDataRequested.Adapters;

internal sealed class CatalogSearchCandidateProjectionPollingService(IServiceScopeFactory scopeFactory) : BackgroundService
{
    private const string CheckpointId = "projector/checkpoints/catalog-search-candidates";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessBatchAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var documentStore = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

        using var session = documentStore.OpenAsyncSession();
        var checkpoint = await session.LoadAsync<ProjectorCheckpointRecordDto>(CheckpointId, cancellationToken)
            ?? new ProjectorCheckpointRecordDto { Id = CheckpointId };

        var query = session.Query<RavenStoredEventRecord>()
            .Where(x => x.AggregateType == "artist-catalog");

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
            switch (storedEvent.Body)
            {
                case ArtistDiscoveredEventDataRecordDto artist when !string.IsNullOrWhiteSpace(artist.ArtistId) && !string.IsNullOrWhiteSpace(artist.ArtistName):
                    await session.StoreAsync(
                        new CatalogSearchCandidateRecordDto
                        {
                            Id = CatalogSearchCandidateRecordDto.GetDocumentId(artist.ArtistId),
                            CatalogItemId = artist.ArtistId,
                            CandidateKind = "Artist",
                            SearchText = artist.ArtistName,
                            UpdatedAt = storedEvent.OccurredAtUtc
                        },
                        cancellationToken);
                    break;

                case AlbumDiscoveredEventDataRecordDto album when !string.IsNullOrWhiteSpace(album.AlbumId) && !string.IsNullOrWhiteSpace(album.AlbumTitle):
                    await session.StoreAsync(
                        new CatalogSearchCandidateRecordDto
                        {
                            Id = CatalogSearchCandidateRecordDto.GetDocumentId(album.AlbumId),
                            CatalogItemId = album.AlbumId,
                            CandidateKind = "Album",
                            SearchText = album.AlbumTitle,
                            UpdatedAt = storedEvent.OccurredAtUtc
                        },
                        cancellationToken);
                    break;

                case TrackDiscoveredEventDataRecordDto track when !string.IsNullOrWhiteSpace(track.MusicCatalogId):
                    await session.StoreAsync(
                        new CatalogSearchCandidateRecordDto
                        {
                            Id = CatalogSearchCandidateRecordDto.GetDocumentId(track.MusicCatalogId),
                            CatalogItemId = track.MusicCatalogId,
                            CandidateKind = "Track",
                            SearchText = $"{track.Title} {track.Artist}".Trim(),
                            UpdatedAt = storedEvent.OccurredAtUtc
                        },
                        cancellationToken);
                    break;
            }

            checkpoint.LastOccurredAtUtc = storedEvent.OccurredAtUtc;
            checkpoint.LastEventId = storedEvent.EventId;
        }

        await session.StoreAsync(checkpoint, cancellationToken);
        await session.SaveChangesAsync(cancellationToken);
    }
}
