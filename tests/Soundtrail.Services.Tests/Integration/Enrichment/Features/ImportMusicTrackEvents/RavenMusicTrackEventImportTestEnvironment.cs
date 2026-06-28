using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog.Commands;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Projection;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicTrackEventsImported;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.Adapters;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Services.Tests.Support;

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
        using var session = this.raven.Store.OpenAsyncSession();
        var handler = new MusicTrackEventsImportedHandler(TestEventStreamRepositories.CreateMusicTrack(session));
        await handler.Handle(command, CancellationToken.None);
        await session.SaveChangesAsync(CancellationToken.None);
    }

    public async Task<CatalogTrackRecordDto?> LoadCatalogTrackAsync(string trackId)
    {
        using var session = this.raven.Store.OpenAsyncSession();
        return await session.LoadAsync<CatalogTrackRecordDto>(
            CatalogTrackRecordDto.GetDocumentId(trackId),
            CancellationToken.None);
    }

    public async Task<CatalogArtistRecordDto?> LoadCatalogArtistAsync(string artistId)
    {
        using var session = this.raven.Store.OpenAsyncSession();
        return await session.LoadAsync<CatalogArtistRecordDto>(
            CatalogArtistRecordDto.GetDocumentId(artistId),
            CancellationToken.None);
    }

    public async Task<CatalogAlbumRecordDto?> LoadCatalogAlbumAsync(string albumId)
    {
        using var session = this.raven.Store.OpenAsyncSession();
        return await session.LoadAsync<CatalogAlbumRecordDto>(
            CatalogAlbumRecordDto.GetDocumentId(albumId),
            CancellationToken.None);
    }

    public async Task ReplayCatalogProjectionAsync()
    {
        using var session = this.raven.Store.OpenAsyncSession();
        var projectHandler = new MusicCatalogChangedHandler(
            new RavenLoadMusicTrackCatalogProjection(session, new RavenMusicTrackCatalogProjectionMapper()),
            new RavenSaveMusicTrackCatalogProjection(session, Soundtrail.Adapters.Registry.TypeTranslationRegistry.Default));
        var streamMetadata = await session.Advanced.LoadStartingWithAsync<RavenEventStreamMetadataRecord>(
            "music-track-streams/");
        var musicCatalogIds = streamMetadata.Select(x => x.StreamId).ToList();

        foreach (var musicCatalogId in musicCatalogIds.Distinct(StringComparer.Ordinal))
        {
            var eventsToReplay = (await session.Advanced.LoadStartingWithAsync<RavenStoredEventRecord>(
                    $"music-track-events/{musicCatalogId}/"))
                .OrderBy(x => x.Version)
                .Select(x => new VersionedMusicTrackEvent(x.Version, TypeTranslationRegistry.Default.ToDomainObject<IMusicTrackEvent>(x.Body!)))
                .ToArray();
            await projectHandler.Handle(
                new MusicCatalogChangedCommand(MusicCatalogId.From(musicCatalogId), eventsToReplay),
                CancellationToken.None);
        }
    }

    public ValueTask DisposeAsync()
    {
        this.raven.Dispose();
        return ValueTask.CompletedTask;
    }
}
