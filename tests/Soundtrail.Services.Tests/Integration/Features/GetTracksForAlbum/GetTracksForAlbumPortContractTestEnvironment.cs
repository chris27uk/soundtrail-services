using Raven.Client.Documents;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;
using Soundtrail.Services.Api.Shared.Contract;
using Soundtrail.Services.Tests.Integration.Shared.Infrastructure;
using Soundtrail.Services.Tests.Integration.Features.GetTracksForAlbum.Support;

namespace Soundtrail.Services.Tests.Integration.Features.GetTracksForAlbum;

internal sealed class GetTracksForAlbumPortContractTestEnvironment : IAsyncDisposable
{
    private readonly IDocumentStore? documentStore;
    private readonly List<string> cleanupDocumentIds;

    private GetTracksForAlbumPortContractTestEnvironment(
        IGetTracksForAlbumPort subject,
        AlbumId albumId,
        IDocumentStore? documentStore = null,
        List<string>? cleanupDocumentIds = null)
    {
        Subject = subject;
        AlbumId = albumId;
        this.documentStore = documentStore;
        this.cleanupDocumentIds = cleanupDocumentIds ?? [];
    }

    public IGetTracksForAlbumPort Subject { get; }

    public AlbumId AlbumId { get; }

    public static async Task<GetTracksForAlbumPortContractTestEnvironment> ForExistingAlbumTracks(
        GetTracksForAlbumPortImplementation implementation,
        string artistId = "artist-1101",
        string albumId = "album-1201",
        string albumTitle = "The Album",
        string? trackId = null,
        string musicCatalogId = "track-1301",
        string title = "The Track",
        string artistName = "The Artist",
        int? durationMs = 201000,
        string? isrc = "GBAYE2401301",
        DateOnly? releaseDate = null,
        string? artworkUrl = "https://cdn.soundtrail.test/tracks/track-1301.jpg")
    {
        if (implementation == GetTracksForAlbumPortImplementation.Fake)
        {
            var resolvedAlbumId = AlbumId.From(artistId, albumId);
            var trackIdValue = trackId ?? global::Soundtrail.Services.Tests.TestTrackIds.Value("track-1301");
            var resolvedTrackId = TrackId.From(trackIdValue);
            var response = new GetTracksForAlbumResponse(
                ArtistId.From(artistId),
                resolvedAlbumId,
                albumTitle,
                [
                    new GetTracksForAlbumTrackResponse(
                        resolvedTrackId,
                        title,
                        artistName,
                        durationMs,
                        isrc,
                        releaseDate ?? new DateOnly(2024, 1, 2),
                        artworkUrl,
                        false,
                        [])
                ]);

            return new GetTracksForAlbumPortContractTestEnvironment(
                new GetTracksForAlbumPortFake(response),
                resolvedAlbumId);
        }

        if (implementation != GetTracksForAlbumPortImplementation.Raven)
        {
            throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null);
        }

        var isolation = EmbeddedRavenTestServer.NewIsolationKey();
        var uniqueArtistId = $"{artistId}-{isolation}";
        var uniqueAlbumId = $"{albumId}-{isolation}";
        var ravenTrackIdValue = global::Soundtrail.Services.Tests.TestTrackIds.Value($"track-1301-{isolation}");
        var ravenAlbumId = AlbumId.From(uniqueArtistId, uniqueAlbumId);
        var resolvedReleaseDate = releaseDate ?? new DateOnly(2024, 1, 2);

        return await CreateRavenEnvironmentAsync(
            ravenAlbumId,
            new CatalogAlbumTracksRecordDto
            {
                Id = CatalogAlbumTracksRecordDto.GetDocumentId(ravenAlbumId.StableValue),
                ArtistId = uniqueArtistId,
                AlbumId = uniqueAlbumId,
                AlbumTitle = albumTitle,
                Tracks =
                [
                    new CatalogAlbumTrackRecordDto
                    {
                        TrackId = ravenTrackIdValue,
                        MusicCatalogId = $"{musicCatalogId}-{isolation}",
                        Title = title,
                        ArtistName = artistName,
                        DurationMs = durationMs,
                        Isrc = isrc,
                        ReleaseDate = resolvedReleaseDate,
                        ArtworkUrl = artworkUrl
                    }
                ]
            });
    }

    public static async Task<GetTracksForAlbumPortContractTestEnvironment> ForMissingAlbumTracks(
        GetTracksForAlbumPortImplementation implementation,
        AlbumId? albumId = null)
    {
        if (implementation == GetTracksForAlbumPortImplementation.Fake)
        {
            var resolvedAlbumId = albumId ?? AlbumId.From("artist-1102", "album-1202");
            return new GetTracksForAlbumPortContractTestEnvironment(
                new GetTracksForAlbumPortFake(),
                resolvedAlbumId);
        }

        if (implementation != GetTracksForAlbumPortImplementation.Raven)
        {
            throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null);
        }

        var isolation = EmbeddedRavenTestServer.NewIsolationKey();
        var ravenAlbumId = AlbumId.From($"artist-1102-{isolation}", $"album-1202-{isolation}");
        return await CreateRavenEnvironmentAsync(ravenAlbumId);
    }

    public async ValueTask DisposeAsync()
    {
        if (documentStore is null)
        {
            return;
        }

        await EmbeddedRavenTestServer.DeleteDocumentsAsync(documentStore, cleanupDocumentIds);
        await EmbeddedRavenTestServer.DisposeAsync(documentStore);
    }

    private static async Task<GetTracksForAlbumPortContractTestEnvironment> CreateRavenEnvironmentAsync(
        AlbumId albumId,
        CatalogAlbumTracksRecordDto? existingRecord = null)
    {
        var store = EmbeddedRavenTestServer.CreateDocumentStore();
        var cleanupDocumentIds = new List<string>();

        if (existingRecord is not null)
        {
            cleanupDocumentIds.Add(existingRecord.Id);
            using var session = store.OpenAsyncSession();
            await session.StoreAsync(existingRecord, existingRecord.Id);
            await session.SaveChangesAsync();
        }

        return new GetTracksForAlbumPortContractTestEnvironment(
            new RavenGetTracksForAlbumPort(store, new TypeRegistryFake()),
            albumId,
            store,
            cleanupDocumentIds);
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
                            track.Title,
                            track.ArtistName,
                            track.DurationMs,
                            track.Isrc,
                            track.ReleaseDate,
                            track.ArtworkUrl,
                            track.StreamingLocations.Length > 0,
                            track.StreamingLocations
                                .Select(static location => new StreamingLocationResponse(
                                    location.Provider,
                                    location.ExternalId,
                                    location.Url))
                                .ToArray()))
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
