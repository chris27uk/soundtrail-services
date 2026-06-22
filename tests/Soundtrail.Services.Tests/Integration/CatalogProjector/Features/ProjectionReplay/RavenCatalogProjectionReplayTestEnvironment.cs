using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog;
using Soundtrail.Services.Api.Infrastructure.Raven;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog.Adapters;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using System.Reflection;

namespace Soundtrail.Services.Tests.Integration.CatalogProjector.Features.ProjectionReplay;

internal sealed class RavenCatalogProjectionReplayTestEnvironment : IAsyncDisposable
{
    private readonly RavenEmbeddedTestDatabase raven;

    private RavenCatalogProjectionReplayTestEnvironment(RavenEmbeddedTestDatabase raven)
    {
        this.raven = raven;
        Search = new RavenCatalogSearch(raven.Store);
    }

    public RavenCatalogSearch Search { get; }

    public static RavenCatalogProjectionReplayTestEnvironment Create()
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        ExecuteIndexes(raven.Store);
        return new RavenCatalogProjectionReplayTestEnvironment(raven);
    }

    public async Task ApplyAsync(params MusicTrackStoredEventRecordDto[] storedEvents)
    {
        using var session = raven.Store.OpenAsyncSession();
        session.Advanced.WaitForIndexesAfterSaveChanges();
        var handler = new ProjectMusicTrackCatalogHandler(
            new RavenLoadMusicTrackCatalogProjection(session, new RavenMusicTrackCatalogProjectionMapper()),
            new RavenSaveMusicTrackCatalogProjection(session, new RavenMusicTrackCatalogProjectionMapper()));

        foreach (var stream in storedEvents.OrderBy(x => x.MusicCatalogId, StringComparer.Ordinal).ThenBy(x => x.Version).GroupBy(x => x.MusicCatalogId, StringComparer.Ordinal))
        {
            await handler.Handle(
                new ProjectMusicTrackCatalogCommand(
                    MusicCatalogId.From(stream.Key),
                    stream.Select(item => new VersionedMusicTrackEvent(item.Version, item.ToDomainEvent())).ToArray()),
                CancellationToken.None);
        }
    }

    public async Task AppendStoredEventsAsync(params MusicTrackStoredEventRecordDto[] storedEvents)
    {
        using var session = raven.Store.OpenAsyncSession();
        session.Advanced.WaitForIndexesAfterSaveChanges();

        foreach (var storedEvent in storedEvents)
        {
            await session.StoreAsync(storedEvent, storedEvent.Id, CancellationToken.None);
        }

        await session.SaveChangesAsync(CancellationToken.None);
    }

    public async Task<int> ReplayAsync(MusicCatalogId musicCatalogId)
    {
        using var session = raven.Store.OpenAsyncSession();
        session.Advanced.WaitForIndexesAfterSaveChanges();
        var eventsToReplay = (await session.Advanced.LoadStartingWithAsync<MusicTrackStoredEventRecordDto>(
                $"music-track-events/{musicCatalogId.Value}/"))
            .OrderBy(x => x.Version)
            .Select(x => new VersionedMusicTrackEvent(x.Version, x.ToDomainEvent()))
            .ToArray();
        var handler = new ProjectMusicTrackCatalogHandler(
            new RavenLoadMusicTrackCatalogProjection(session, new RavenMusicTrackCatalogProjectionMapper()),
            new RavenSaveMusicTrackCatalogProjection(session, new RavenMusicTrackCatalogProjectionMapper()));

        await handler.Handle(
            new ProjectMusicTrackCatalogCommand(musicCatalogId, eventsToReplay),
            CancellationToken.None);

        return eventsToReplay.Length;
    }

    public async Task<CatalogTrackRecordDto?> LoadTrackAsync(string trackId)
    {
        using var session = raven.Store.OpenAsyncSession();
        return await session.LoadAsync<CatalogTrackRecordDto>(CatalogTrackRecordDto.GetDocumentId(trackId), CancellationToken.None);
    }

    public async Task<CatalogArtistRecordDto?> LoadArtistAsync(string artistId)
    {
        using var session = raven.Store.OpenAsyncSession();
        return await session.LoadAsync<CatalogArtistRecordDto>(CatalogArtistRecordDto.GetDocumentId(artistId), CancellationToken.None);
    }

    public async Task<CatalogAlbumRecordDto?> LoadAlbumAsync(string albumId)
    {
        using var session = raven.Store.OpenAsyncSession();
        return await session.LoadAsync<CatalogAlbumRecordDto>(CatalogAlbumRecordDto.GetDocumentId(albumId), CancellationToken.None);
    }

    public Task<LocalCatalogSearchResponse> SearchAsync(string query, string? types = null, string? playback = null) =>
        Search.SearchAsync(
            new SearchCatalogCommand(
                NormalizedSearchQuery.FromText(query),
                SearchTypesFilter.Parse(types),
                PlaybackProviderFilter.Parse(playback),
                SearchLimit.From(25),
                SearchOffset.From(0)),
            CancellationToken.None);

    public ValueTask DisposeAsync()
    {
        raven.Dispose();
        return ValueTask.CompletedTask;
    }

    private static void ExecuteIndexes(IDocumentStore store)
    {
        foreach (var type in IndexTypes)
        {
            var index = (AbstractIndexCreationTask)Activator.CreateInstance(type)!;
            index.Execute(store);
        }
    }

    private static readonly Assembly ApiAssembly = typeof(Soundtrail.Services.Api.ApiAssemblyMarker).Assembly;

    private static readonly IReadOnlyList<Type> IndexTypes =
    [
        ApiAssembly.GetType("Soundtrail.Services.Api.Infrastructure.Raven.Indexes.Search_Artists", true)!,
        ApiAssembly.GetType("Soundtrail.Services.Api.Infrastructure.Raven.Indexes.Search_Albums", true)!,
        ApiAssembly.GetType("Soundtrail.Services.Api.Infrastructure.Raven.Indexes.Search_Tracks", true)!,
        ApiAssembly.GetType("Soundtrail.Services.Api.Infrastructure.Raven.Indexes.Artists_ByMusicBrainzId", true)!,
        ApiAssembly.GetType("Soundtrail.Services.Api.Infrastructure.Raven.Indexes.Albums_ByArtistAndName", true)!,
        ApiAssembly.GetType("Soundtrail.Services.Api.Infrastructure.Raven.Indexes.Albums_ByMusicBrainzReleaseId", true)!,
        ApiAssembly.GetType("Soundtrail.Services.Api.Infrastructure.Raven.Indexes.Tracks_ByArtistAlbumAndName", true)!,
        ApiAssembly.GetType("Soundtrail.Services.Api.Infrastructure.Raven.Indexes.Tracks_ByMusicBrainzRecordingId", true)!,
        ApiAssembly.GetType("Soundtrail.Services.Api.Infrastructure.Raven.Indexes.Tracks_ByIsrc", true)!
    ];
}
