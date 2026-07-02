using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Session;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Commands;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Projection;
using Soundtrail.Services.Api;
using Soundtrail.Services.Api.Infrastructure.Raven;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.Adapters;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Services.Tests.Support;
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

    public async Task ApplyAsync(params CatalogReplayEvent[] storedEvents)
    {
        using var session = raven.Store.OpenAsyncSession();
        session.Advanced.WaitForIndexesAfterSaveChanges();
        var handler = new MusicCatalogChangedHandler(
            new RavenSaveMusicTrackCatalogProjection(session, TypeTranslationRegistry.Default));

        foreach (var stream in storedEvents.GroupBy(x => x.ArtistId, StringComparer.Ordinal).OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            await handler.Handle(
                new MusicCatalogChangedCommand(
                    ArtistId.From(stream.Key),
                    stream.OrderBy(item => item.Version)
                        .Select(item => new VersionedCatalogEvent(item.Version, EnrichLegacyEvent(item)))
                        .ToArray()),
                CancellationToken.None);
        }
    }

    public async Task AppendStoredEventsAsync(params CatalogReplayEvent[] storedEvents)
    {
        using var session = raven.Store.OpenAsyncSession();
        session.Advanced.WaitForIndexesAfterSaveChanges();
        var repository = TestEventStreamRepositories.CreateArtistCatalog(session);

        foreach (var stream in storedEvents
                     .GroupBy(x => x.ArtistId, StringComparer.Ordinal)
                     .OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            var events = stream.OrderBy(x => x.Version)
                .Select(EnrichLegacyEvent)
                .ToArray();

            await repository.AppendAsync(
                LoadedEventStream<ArtistId, IDomainEvent>.Empty(ArtistId.From(stream.Key)),
                events,
                OperationId.From($"CatalogReplay:{stream.Key}"),
                CancellationToken.None);
        }

        await session.SaveChangesAsync(CancellationToken.None);
    }

    public async Task<int> ReplayAsync(MusicCatalogId musicCatalogId)
    {
        using var session = raven.Store.OpenAsyncSession();
        session.Advanced.WaitForIndexesAfterSaveChanges();
        var artistId = await ResolveArtistIdAsync(session, musicCatalogId, CancellationToken.None);
        var repository = TestEventStreamRepositories.CreateArtistCatalog(session);
        var loaded = await repository.LoadAsync(artistId, CancellationToken.None);
        var eventsToReplay = loaded.Events
            .Select((@event, index) => new VersionedCatalogEvent(index + 1, @event))
            .ToArray();
        var handler = new MusicCatalogChangedHandler(
            new RavenSaveMusicTrackCatalogProjection(session, Soundtrail.Adapters.Registry.TypeTranslationRegistry.Default));

        await handler.Handle(
            new Domain.Catalog.Commands.MusicCatalogChangedCommand(artistId, eventsToReplay),
            CancellationToken.None);

        return eventsToReplay.Length;
    }

    private static async Task<ArtistId> ResolveArtistIdAsync(IAsyncDocumentSession session, MusicCatalogId musicCatalogId, CancellationToken cancellationToken)
    {
        var repository = TestEventStreamRepositories.CreateArtistCatalog(session);
        var track = await session.LoadAsync<CatalogTrackRecordDto>(
            CatalogTrackRecordDto.GetDocumentId(musicCatalogId.Value),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(track?.ArtistId))
        {
            return ArtistId.From(track.ArtistId);
        }

        var streamMetadata = await session.Advanced.LoadStartingWithAsync<RavenEventStreamMetadataRecord>(
            "artist-catalog-streams/",
            pageSize: 512);

        foreach (var metadata in streamMetadata.OrderBy(x => x.StreamId, StringComparer.Ordinal))
        {
            var loaded = await repository.LoadAsync(ArtistId.From(metadata.StreamId), cancellationToken);
            var storedEvent = loaded.Events
                .OfType<TrackDiscovered>()
                .FirstOrDefault(x => x.MusicCatalogId == musicCatalogId);

            if (storedEvent is null)
            {
                continue;
            }

            return ArtistId.From(metadata.StreamId);
        }

        throw new InvalidOperationException($"Could not resolve artist stream for '{musicCatalogId.Value}'.");
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
                MusicIdentityText.NormalizeFreeText(query),
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

    private static readonly Assembly ApiAssembly = typeof(ApiAssemblyMarker).Assembly;

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

    public sealed record CatalogReplayEvent(string MusicCatalogId, string ArtistId, int Version, IMusicTrackEvent Event);

    private static IDomainEvent EnrichLegacyEvent(CatalogReplayEvent replayEvent)
    {
        var musicCatalogId = MusicCatalogId.From(replayEvent.MusicCatalogId);

        return replayEvent.Event switch
        {
            TrackDiscovered discovered when discovered.MusicCatalogId is null =>
                discovered with { MusicCatalogId = musicCatalogId },
            ProviderReferenceDiscovered discovered when discovered.MusicCatalogId is null =>
                discovered with { MusicCatalogId = musicCatalogId },
            ProviderReferenceLookupFailed failed when failed.MusicCatalogId is null =>
                failed with { MusicCatalogId = musicCatalogId },
            MetadataCorrected corrected when corrected.MusicCatalogId is null =>
                corrected with { MusicCatalogId = musicCatalogId },
            _ => replayEvent.Event
        };
    }
}
