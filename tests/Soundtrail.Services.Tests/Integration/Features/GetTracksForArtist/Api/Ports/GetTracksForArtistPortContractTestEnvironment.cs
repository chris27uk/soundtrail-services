using Raven.Client.Documents;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Contract;
using Soundtrail.Services.Tests.Integration.Shared.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.GetTracksForArtist.Api.Ports;

internal sealed class GetTracksForArtistPortContractTestEnvironment : IAsyncDisposable
{
    private readonly IDocumentStore? documentStore;
    private readonly List<string> cleanupDocumentIds;

    private GetTracksForArtistPortContractTestEnvironment(
        IGetTracksForArtistPort subject,
        ArtistId artistId,
        IDocumentStore? documentStore = null,
        List<string>? cleanupDocumentIds = null)
    {
        Subject = subject;
        ArtistId = artistId;
        this.documentStore = documentStore;
        this.cleanupDocumentIds = cleanupDocumentIds ?? [];
    }

    public IGetTracksForArtistPort Subject { get; }

    public ArtistId ArtistId { get; }

    public static async Task<GetTracksForArtistPortContractTestEnvironment> ForExistingArtistTracks(
        GetTracksForArtistPortImplementation implementation,
        string artistId = "artist-2701",
        string artistName = "The Artist",
        string? trackId = null,
        string musicCatalogId = "track-2801",
        string title = "The Track",
        string trackArtistName = "The Artist",
        string? albumTitle = "The Album",
        int? durationMs = 201000,
        string? isrc = "GBAYE2402801",
        DateOnly? releaseDate = null,
        string? artworkUrl = "https://cdn.soundtrail.test/tracks/track-2801.jpg")
    {
        if (implementation == GetTracksForArtistPortImplementation.Fake)
        {
            var resolvedArtistId = ArtistId.From(artistId);
            var trackIdValue = trackId ?? global::Soundtrail.Services.Tests.TestTrackIds.Value("track-2801");
            var resolvedTrackId = TrackId.From(trackIdValue);
            var response = new GetTracksForArtistResponse(
                resolvedArtistId,
                ArtistName.From(artistName),
                [
                    new GetTracksForArtistTrackResponse(
                        resolvedTrackId,
                        title,
                        trackArtistName,
                        albumTitle,
                        durationMs,
                        isrc,
                        releaseDate ?? new DateOnly(2024, 1, 2),
                        artworkUrl,
                        false,
                        [])
                ]);

            return new GetTracksForArtistPortContractTestEnvironment(
                new GetTracksForArtistPortFake(response),
                resolvedArtistId);
        }

        if (implementation != GetTracksForArtistPortImplementation.Raven)
        {
            throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null);
        }

        var isolation = EmbeddedRavenTestServer.NewIsolationKey();
        var uniqueArtistId = $"{artistId}-{isolation}";
        var ravenTrackIdValue = global::Soundtrail.Services.Tests.TestTrackIds.Value($"track-2801-{isolation}");
        var ravenArtistId = ArtistId.From(uniqueArtistId);
        var resolvedReleaseDate = releaseDate ?? new DateOnly(2024, 1, 2);

        return await CreateRavenEnvironmentAsync(
            ravenArtistId,
            new CatalogArtistTracksRecordDto
            {
                Id = CatalogArtistTracksRecordDto.GetDocumentId(uniqueArtistId),
                ArtistId = uniqueArtistId,
                ArtistName = artistName,
                Tracks =
                [
                    new CatalogArtistTrackRecordDto
                    {
                        TrackId = ravenTrackIdValue,
                        MusicCatalogId = $"{musicCatalogId}-{isolation}",
                        Title = title,
                        ArtistName = trackArtistName,
                        AlbumTitle = albumTitle,
                        DurationMs = durationMs,
                        Isrc = isrc,
                        ReleaseDate = resolvedReleaseDate,
                        ArtworkUrl = artworkUrl
                    }
                ]
            });
    }

    public static async Task<GetTracksForArtistPortContractTestEnvironment> ForMissingArtistTracks(
        GetTracksForArtistPortImplementation implementation,
        ArtistId? artistId = null)
    {
        if (implementation == GetTracksForArtistPortImplementation.Fake)
        {
            var resolvedArtistId = artistId ?? ArtistId.From("artist-2702");
            return new GetTracksForArtistPortContractTestEnvironment(
                new GetTracksForArtistPortFake(),
                resolvedArtistId);
        }

        if (implementation != GetTracksForArtistPortImplementation.Raven)
        {
            throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null);
        }

        var isolation = EmbeddedRavenTestServer.NewIsolationKey();
        var ravenArtistId = ArtistId.From($"artist-2702-{isolation}");
        return await CreateRavenEnvironmentAsync(ravenArtistId);
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

    private static async Task<GetTracksForArtistPortContractTestEnvironment> CreateRavenEnvironmentAsync(
        ArtistId artistId,
        CatalogArtistTracksRecordDto? existingRecord = null)
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

        return new GetTracksForArtistPortContractTestEnvironment(
            new RavenGetTracksForArtistPort(store, new TypeRegistryFake()),
            artistId,
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
            var record = (CatalogArtistTracksRecordDto)dto!;
            return new GetTracksForArtistResponse(
                ArtistId.From(record.ArtistId),
                ArtistName.From(record.ArtistName),
                record.Tracks.Select(
                        track => new GetTracksForArtistTrackResponse(
                            TrackId.From(track.TrackId),
                            track.Title,
                            track.ArtistName,
                            track.AlbumTitle,
                            track.DurationMs,
                            track.Isrc,
                            track.ReleaseDate,
                            track.ArtworkUrl,
                            track.StreamingLocations.Length > 0,
                            track.StreamingLocations
                                .Select(static location => new Soundtrail.Services.Api.Shared.Contract.StreamingLocationResponse(
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

public enum GetTracksForArtistPortImplementation
{
    Fake,
    Raven
}
