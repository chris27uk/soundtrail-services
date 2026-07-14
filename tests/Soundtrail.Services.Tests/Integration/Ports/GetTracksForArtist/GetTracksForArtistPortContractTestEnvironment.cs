using Raven.Client.Documents;
using Raven.Embedded;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.GetTracksForArtist.Adapters;
using Soundtrail.Services.Api.Features.GetTracksForArtist.Contract;

namespace Soundtrail.Services.Tests.Integration.Ports.GetTracksForArtist;

internal sealed class GetTracksForArtistPortContractTestEnvironment : IAsyncDisposable
{
    private static int serverStarted;
    private readonly IDocumentStore? documentStore;

    private GetTracksForArtistPortContractTestEnvironment(
        IGetTracksForArtistPort subject,
        ArtistId artistId,
        IDocumentStore? documentStore = null)
    {
        Subject = subject;
        ArtistId = artistId;
        this.documentStore = documentStore;
    }

    public IGetTracksForArtistPort Subject { get; }

    public ArtistId ArtistId { get; }

    public static async Task<GetTracksForArtistPortContractTestEnvironment> ForExistingArtistTracks(
        GetTracksForArtistPortImplementation implementation,
        string artistId = "artist-2701",
        string artistName = "The Artist",
        string trackId = "track-2801",
        string musicCatalogId = "track-2801",
        string title = "The Track",
        string trackArtistName = "The Artist",
        string? albumTitle = "The Album",
        int? durationMs = 201000,
        string? isrc = "GBAYE2402801",
        DateOnly? releaseDate = null,
        string? artworkUrl = "https://cdn.soundtrail.test/tracks/track-2801.jpg")
    {
        var resolvedArtistId = ArtistId.From(artistId);
        var resolvedTrackId = TrackId.From(trackId);
        var response = new GetTracksForArtistResponse(
            resolvedArtistId,
            ArtistName.From(artistName),
            [
                new GetTracksForArtistTrackResponse(
                    resolvedTrackId,
                    new CatalogItemId.Track(resolvedTrackId),
                    title,
                    trackArtistName,
                    albumTitle,
                    durationMs,
                    isrc,
                    releaseDate ?? new DateOnly(2024, 1, 2),
                    artworkUrl)
            ]);

        return implementation switch
        {
            GetTracksForArtistPortImplementation.Fake => new GetTracksForArtistPortContractTestEnvironment(
                new GetTracksForArtistPortFake(response),
                resolvedArtistId),
            GetTracksForArtistPortImplementation.Raven => await CreateRavenEnvironmentAsync(
                resolvedArtistId,
                new CatalogArtistTracksRecordDto
                {
                    Id = CatalogArtistTracksRecordDto.GetDocumentId(artistId),
                    ArtistId = artistId,
                    ArtistName = artistName,
                    Tracks =
                    [
                        new CatalogArtistTrackRecordDto
                        {
                            TrackId = trackId,
                            MusicCatalogId = musicCatalogId,
                            Title = title,
                            ArtistName = trackArtistName,
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

    public static async Task<GetTracksForArtistPortContractTestEnvironment> ForMissingArtistTracks(
        GetTracksForArtistPortImplementation implementation,
        ArtistId? artistId = null)
    {
        var resolvedArtistId = artistId ?? ArtistId.From("artist-2702");

        return implementation switch
        {
            GetTracksForArtistPortImplementation.Fake => new GetTracksForArtistPortContractTestEnvironment(
                new GetTracksForArtistPortFake(),
                resolvedArtistId),
            GetTracksForArtistPortImplementation.Raven => await CreateRavenEnvironmentAsync(resolvedArtistId),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };
    }

    public ValueTask DisposeAsync()
    {
        documentStore?.Dispose();
        return ValueTask.CompletedTask;
    }

    private static async Task<GetTracksForArtistPortContractTestEnvironment> CreateRavenEnvironmentAsync(
        ArtistId artistId,
        CatalogArtistTracksRecordDto? existingRecord = null)
    {
        EnsureEmbeddedServerStarted();
        var store = EmbeddedServer.Instance.GetDocumentStore($"soundtrail-services-tests-{Guid.NewGuid():N}");

        if (existingRecord is not null)
        {
            using var session = store.OpenAsyncSession();
            await session.StoreAsync(existingRecord, existingRecord.Id);
            await session.SaveChangesAsync();
        }

        return new GetTracksForArtistPortContractTestEnvironment(
            new RavenGetTracksForArtistPort(store, new TypeRegistryFake()),
            artistId,
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
            var record = (CatalogArtistTracksRecordDto)dto!;
            return new GetTracksForArtistResponse(
                ArtistId.From(record.ArtistId),
                ArtistName.From(record.ArtistName),
                record.Tracks.Select(
                        track => new GetTracksForArtistTrackResponse(
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

public enum GetTracksForArtistPortImplementation
{
    Fake,
    Raven
}
