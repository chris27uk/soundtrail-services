using Raven.Client.Documents;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog;
using Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ImportMusicTrackEvents;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using System.Linq;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Features.ImportMusicTrackEvents;

internal sealed class RavenMusicTrackEventImportTestEnvironment : IAsyncDisposable
{
    private readonly RavenEmbeddedTestDatabase raven;

    private RavenMusicTrackEventImportTestEnvironment(RavenEmbeddedTestDatabase raven)
    {
        this.raven = raven;
    }

    public static RavenMusicTrackEventImportTestEnvironment Create() => new(RavenEmbeddedTestDatabase.Create());

    public async Task ImportAsync(ImportMusicTrackEventsCommand command)
    {
        using var session = raven.Store.OpenAsyncSession();
        var handler = new ImportMusicTrackEventsHandler(new RavenMusicTrackStreamStore(session));
        await handler.Handle(command, CancellationToken.None);
        await session.SaveChangesAsync(CancellationToken.None);
    }

    public async Task<CatalogTrackRecordDto?> LoadCatalogTrackAsync(string trackId)
    {
        using var session = raven.Store.OpenAsyncSession();
        return await session.LoadAsync<CatalogTrackRecordDto>(
            CatalogTrackRecordDto.GetDocumentId(trackId),
            CancellationToken.None);
    }

    public async Task<CatalogArtistRecordDto?> LoadCatalogArtistAsync(string artistId)
    {
        using var session = raven.Store.OpenAsyncSession();
        return await session.LoadAsync<CatalogArtistRecordDto>(
            CatalogArtistRecordDto.GetDocumentId(artistId),
            CancellationToken.None);
    }

    public async Task<CatalogAlbumRecordDto?> LoadCatalogAlbumAsync(string albumId)
    {
        using var session = raven.Store.OpenAsyncSession();
        return await session.LoadAsync<CatalogAlbumRecordDto>(
            CatalogAlbumRecordDto.GetDocumentId(albumId),
            CancellationToken.None);
    }

    public async Task ReplayCatalogProjectionAsync()
    {
        using var session = raven.Store.OpenAsyncSession();
        var projectHandler = new ProjectMusicTrackCatalogHandler(
            new RavenLoadMusicTrackCatalogProjection(session, new RavenMusicTrackCatalogProjectionMapper()),
            new RavenSaveMusicTrackCatalogProjection(session, new RavenMusicTrackCatalogProjectionMapper()));
        var streamMetadata = await session.Advanced.LoadStartingWithAsync<MusicTrackEventStreamMetadataRecordDto>(
            "music-track-streams/");
        var musicCatalogIds = streamMetadata.Select(x => x.MusicCatalogId).ToList();

        foreach (var musicCatalogId in musicCatalogIds.Distinct(StringComparer.Ordinal))
        {
            var eventsToReplay = (await session.Advanced.LoadStartingWithAsync<MusicTrackStoredEventRecordDto>(
                    $"music-track-events/{musicCatalogId}/"))
                .OrderBy(x => x.Version)
                .Select(x => new VersionedMusicTrackEvent(x.Version, x.ToDomainEvent()))
                .ToArray();
            await projectHandler.Handle(
                new ProjectMusicTrackCatalogCommand(MusicCatalogId.From(musicCatalogId), eventsToReplay),
                CancellationToken.None);
        }
    }

    public ValueTask DisposeAsync()
    {
        raven.Dispose();
        return ValueTask.CompletedTask;
    }
}
