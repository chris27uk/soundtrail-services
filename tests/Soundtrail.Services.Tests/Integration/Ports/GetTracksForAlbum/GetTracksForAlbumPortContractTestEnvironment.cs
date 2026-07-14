using Raven.Client.Documents;
using Raven.Embedded;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.GetTracksForAlbum.Adapters;
using Soundtrail.Services.Api.Features.GetTracksForAlbum.Contract;

namespace Soundtrail.Services.Tests.Integration.Ports.GetTracksForAlbum;

internal sealed class GetTracksForAlbumPortContractTestEnvironment : IAsyncDisposable
{
    private static int serverStarted;
    private readonly IDocumentStore? documentStore;

    private GetTracksForAlbumPortContractTestEnvironment(
        IGetTracksForAlbumPort subject,
        AlbumId albumId,
        IDocumentStore? documentStore = null)
    {
        Subject = subject;
        AlbumId = albumId;
        this.documentStore = documentStore;
    }

    public IGetTracksForAlbumPort Subject { get; }

    public AlbumId AlbumId { get; }

    public static async Task<GetTracksForAlbumPortContractTestEnvironment> ForExistingAlbumTracks(
        GetTracksForAlbumPortImplementation implementation,
        string artistId = "artist-1101",
        string albumId = "album-1201",
        string albumTitle = "The Album",
        string trackId = "track-1301",
        string musicCatalogId = "track-1301",
        string title = "The Track",
        string artistName = "The Artist",
        int? durationMs = 201000,
        string? isrc = "GBAYE2401301",
        DateOnly? releaseDate = null,
        string? artworkUrl = "https://cdn.soundtrail.test/tracks/track-1301.jpg")
    {
        var resolvedAlbumId = AlbumId.From(artistId, albumId);
        var resolvedTrackId = TrackId.From(trackId);
        var response = new GetTracksForAlbumResponse(
            ArtistId.From(artistId),
            resolvedAlbumId,
            albumTitle,
            [
                new GetTracksForAlbumTrackResponse(
                    resolvedTrackId,
                    new CatalogItemId.Track(resolvedTrackId),
                    title,
                    artistName,
                    durationMs,
                    isrc,
                    releaseDate ?? new DateOnly(2024, 1, 2),
                    artworkUrl)
            ]);

        return implementation switch
        {
            GetTracksForAlbumPortImplementation.Fake => new GetTracksForAlbumPortContractTestEnvironment(
                new GetTracksForAlbumPortFake(response),
                resolvedAlbumId),
            GetTracksForAlbumPortImplementation.Raven => await CreateRavenEnvironmentAsync(
                resolvedAlbumId,
                new CatalogAlbumTracksRecordDto
                {
                    Id = CatalogAlbumTracksRecordDto.GetDocumentId(resolvedAlbumId.StableValue),
                    ArtistId = artistId,
                    AlbumId = albumId,
                    AlbumTitle = albumTitle,
                    Tracks =
                    [
                        new CatalogAlbumTrackRecordDto
                        {
                            TrackId = trackId,
                            MusicCatalogId = musicCatalogId,
                            Title = title,
                            ArtistName = artistName,
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

    public static async Task<GetTracksForAlbumPortContractTestEnvironment> ForMissingAlbumTracks(
        GetTracksForAlbumPortImplementation implementation,
        AlbumId? albumId = null)
    {
        var resolvedAlbumId = albumId ?? AlbumId.From("artist-1102", "album-1202");

        return implementation switch
        {
            GetTracksForAlbumPortImplementation.Fake => new GetTracksForAlbumPortContractTestEnvironment(
                new GetTracksForAlbumPortFake(),
                resolvedAlbumId),
            GetTracksForAlbumPortImplementation.Raven => await CreateRavenEnvironmentAsync(resolvedAlbumId),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };
    }

    public ValueTask DisposeAsync()
    {
        documentStore?.Dispose();
        return ValueTask.CompletedTask;
    }

    private static async Task<GetTracksForAlbumPortContractTestEnvironment> CreateRavenEnvironmentAsync(
        AlbumId albumId,
        CatalogAlbumTracksRecordDto? existingRecord = null)
    {
        EnsureEmbeddedServerStarted();
        var store = EmbeddedServer.Instance.GetDocumentStore($"soundtrail-services-tests-{Guid.NewGuid():N}");

        if (existingRecord is not null)
        {
            using var session = store.OpenAsyncSession();
            await session.StoreAsync(existingRecord, existingRecord.Id);
            await session.SaveChangesAsync();
        }

        return new GetTracksForAlbumPortContractTestEnvironment(
            new RavenGetTracksForAlbumPort(store, new TypeRegistryFake()),
            albumId,
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
            var record = (CatalogAlbumTracksRecordDto)dto!;
            return new GetTracksForAlbumResponse(
                ArtistId.From(record.ArtistId),
                AlbumId.From(record.ArtistId, record.AlbumId),
                record.AlbumTitle,
                record.Tracks.Select(
                        track => new GetTracksForAlbumTrackResponse(
                            TrackId.From(track.TrackId),
                            new CatalogItemId.Track(TrackId.From(track.TrackId)),
                            track.Title,
                            track.ArtistName,
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

public enum GetTracksForAlbumPortImplementation
{
    Fake,
    Raven
}
