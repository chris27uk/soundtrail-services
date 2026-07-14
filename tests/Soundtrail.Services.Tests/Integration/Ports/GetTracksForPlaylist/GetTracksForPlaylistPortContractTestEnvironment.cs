using Raven.Client.Documents;
using Raven.Embedded;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.GetTracksForPlaylist.Adapters;
using Soundtrail.Services.Api.Features.GetTracksForPlaylist.Contract;

namespace Soundtrail.Services.Tests.Integration.Ports.GetTracksForPlaylist;

internal sealed class GetTracksForPlaylistPortContractTestEnvironment : IAsyncDisposable
{
    private static int serverStarted;
    private readonly IDocumentStore? documentStore;

    private GetTracksForPlaylistPortContractTestEnvironment(
        IGetTracksForPlaylistPort subject,
        PlaylistId playlistId,
        IDocumentStore? documentStore = null)
    {
        Subject = subject;
        PlaylistId = playlistId;
        this.documentStore = documentStore;
    }

    public IGetTracksForPlaylistPort Subject { get; }

    public PlaylistId PlaylistId { get; }

    public static async Task<GetTracksForPlaylistPortContractTestEnvironment> ForExistingPlaylistTracks(
        GetTracksForPlaylistPortImplementation implementation,
        string playlistName = "WorldwideSongChart",
        string trackId = "track-3501",
        string musicCatalogId = "track-3501",
        string title = "The Track",
        string artistName = "The Artist",
        string? albumTitle = "The Album",
        int? durationMs = 201000,
        string? isrc = "GBAYE2403501",
        DateOnly? releaseDate = null,
        string? artworkUrl = "https://cdn.soundtrail.test/tracks/track-3501.jpg")
    {
        var resolvedPlaylistId = PlaylistId.FromPlaylistName(playlistName);
        var resolvedTrackId = TrackId.From(trackId);
        var response = new GetTracksForPlaylistResponse(
            resolvedPlaylistId,
            [
                new GetTracksForPlaylistTrackResponse(
                    resolvedTrackId,
                    new CatalogItemId.Track(resolvedTrackId),
                    title,
                    artistName,
                    albumTitle,
                    durationMs,
                    isrc,
                    releaseDate ?? new DateOnly(2024, 1, 2),
                    artworkUrl)
            ]);

        return implementation switch
        {
            GetTracksForPlaylistPortImplementation.Fake => new GetTracksForPlaylistPortContractTestEnvironment(
                new GetTracksForPlaylistPortFake(response),
                resolvedPlaylistId),
            GetTracksForPlaylistPortImplementation.Raven => await CreateRavenEnvironmentAsync(
                resolvedPlaylistId,
                new CatalogPlaylistTracksRecordDto
                {
                    Id = CatalogPlaylistTracksRecordDto.GetDocumentId(resolvedPlaylistId.Value),
                    PlaylistId = resolvedPlaylistId.Value,
                    Tracks =
                    [
                        new CatalogPlaylistTrackRecordDto
                        {
                            TrackId = trackId,
                            MusicCatalogId = musicCatalogId,
                            Title = title,
                            ArtistName = artistName,
                            AlbumTitle = albumTitle,
                            DurationMs = durationMs,
                            Isrc = isrc,
                            ReleaseDate = response.Tracks[0].ReleaseDate,
                            ArtworkUrl = artworkUrl
                        }
                    ]
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };
    }

    public static async Task<GetTracksForPlaylistPortContractTestEnvironment> ForMissingPlaylistTracks(
        GetTracksForPlaylistPortImplementation implementation,
        PlaylistId? playlistId = null)
    {
        var resolvedPlaylistId = playlistId ?? PlaylistId.FromPlaylistName("WorldwideSongChart");

        return implementation switch
        {
            GetTracksForPlaylistPortImplementation.Fake => new GetTracksForPlaylistPortContractTestEnvironment(
                new GetTracksForPlaylistPortFake(),
                resolvedPlaylistId),
            GetTracksForPlaylistPortImplementation.Raven => await CreateRavenEnvironmentAsync(resolvedPlaylistId),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };
    }

    public ValueTask DisposeAsync()
    {
        documentStore?.Dispose();
        return ValueTask.CompletedTask;
    }

    private static async Task<GetTracksForPlaylistPortContractTestEnvironment> CreateRavenEnvironmentAsync(
        PlaylistId playlistId,
        CatalogPlaylistTracksRecordDto? existingRecord = null)
    {
        EnsureEmbeddedServerStarted();
        var store = EmbeddedServer.Instance.GetDocumentStore($"soundtrail-services-tests-{Guid.NewGuid():N}");

        if (existingRecord is not null)
        {
            using var session = store.OpenAsyncSession();
            await session.StoreAsync(existingRecord, existingRecord.Id);
            await session.SaveChangesAsync();
        }

        return new GetTracksForPlaylistPortContractTestEnvironment(
            new RavenGetTracksForPlaylistPort(store, new TypeRegistryFake()),
            playlistId,
            store);
    }

    private static void EnsureEmbeddedServerStarted()
    {
        if (Interlocked.Exchange(ref serverStarted, 1) == 1)
        {
            return;
        }

        try
        {
            EmbeddedServer.Instance.StartServer();
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("already started", StringComparison.OrdinalIgnoreCase))
        {
        }
    }

    private sealed class TypeRegistryFake : ITypeRegistry
    {
        public TDto ToDto<TDto>(object domainObject) where TDto : class => throw new NotSupportedException();

        public object ToDto(object domainObject) => throw new NotSupportedException();

        public TDomain ToDomainObject<TDomain>(object dto) where TDomain : class => (ToDomainObject(dto) as TDomain)!;

        public object ToDomainObject(object? dto)
        {
            var record = (CatalogPlaylistTracksRecordDto)dto!;
            return new GetTracksForPlaylistResponse(
                PlaylistId.FromPlaylistName(record.PlaylistId),
                record.Tracks.Select(
                        track => new GetTracksForPlaylistTrackResponse(
                            TrackId.From(track.TrackId),
                            new CatalogItemId.Track(TrackId.From(track.TrackId)),
                            track.Title,
                            track.ArtistName,
                            track.AlbumTitle,
                            track.DurationMs,
                            track.Isrc,
                            track.ReleaseDate,
                            track.ArtworkUrl))
                    .ToArray());
        }

        public void MapOnto<TSource, TTarget>(TSource source, TTarget target)
            where TSource : class
            where TTarget : class => throw new NotSupportedException();
    }
}

public enum GetTracksForPlaylistPortImplementation
{
    Fake,
    Raven
}
