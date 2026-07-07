using Raven.Client.Documents;
using Raven.Embedded;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Api.Features.GetTrack.Adapters;
using Soundtrail.Services.Api.Features.GetTrack.Contract;

namespace Soundtrail.Services.Tests.Integration.Ports.GetTrack;

internal sealed class GetTrackPortContractTestEnvironment : IAsyncDisposable
{
    private static int serverStarted;
    private readonly IDocumentStore? documentStore;

    private GetTrackPortContractTestEnvironment(
        IGetTrackPort subject,
        TrackId trackId,
        IDocumentStore? documentStore = null)
    {
        Subject = subject;
        TrackId = trackId;
        this.documentStore = documentStore;
    }

    public IGetTrackPort Subject { get; }

    public TrackId TrackId { get; }

    public static async Task<GetTrackPortContractTestEnvironment> ForExistingTrack(
        GetTrackPortImplementation implementation,
        string trackId = "track-601",
        string musicCatalogId = "mc_track_601",
        string title = "The Track",
        string artistName = "The Artist",
        string? albumTitle = "The Album",
        int? durationMs = 201000,
        string? isrc = "GBAYE2400301",
        DateOnly? releaseDate = null,
        string? artworkUrl = "https://cdn.soundtrail.test/tracks/mc_track_601.jpg")
    {
        var resolvedTrackId = TrackId.From(trackId);
        var response = new GetTrackResponse(
            resolvedTrackId,
            new MusicCatalogId.Track(resolvedTrackId),
            title,
            artistName,
            albumTitle,
            durationMs,
            isrc,
            releaseDate ?? new DateOnly(2024, 1, 2),
            artworkUrl);

        return implementation switch
        {
            GetTrackPortImplementation.Fake => new GetTrackPortContractTestEnvironment(
                new GetTrackPortFake(response),
                resolvedTrackId),
            GetTrackPortImplementation.Raven => await CreateRavenEnvironmentAsync(
                resolvedTrackId,
                new CatalogTrackRecordDto
                {
                    Id = CatalogTrackRecordDto.GetDocumentId(trackId),
                    TrackId = trackId,
                    MusicCatalogId = musicCatalogId,
                    Title = title,
                    ArtistName = artistName,
                    AlbumTitle = albumTitle,
                    DurationMs = durationMs,
                    Isrc = isrc,
                    ReleaseDate = response.ReleaseDate,
                    ArtworkUrl = artworkUrl
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };
    }

    public static async Task<GetTrackPortContractTestEnvironment> ForMissingTrack(
        GetTrackPortImplementation implementation,
        TrackId? trackId = null)
    {
        var resolvedTrackId = trackId ?? TrackId.From("track-602");

        return implementation switch
        {
            GetTrackPortImplementation.Fake => new GetTrackPortContractTestEnvironment(
                new GetTrackPortFake(),
                resolvedTrackId),
            GetTrackPortImplementation.Raven => await CreateRavenEnvironmentAsync(resolvedTrackId),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };
    }

    public ValueTask DisposeAsync()
    {
        documentStore?.Dispose();
        return ValueTask.CompletedTask;
    }

    private static async Task<GetTrackPortContractTestEnvironment> CreateRavenEnvironmentAsync(
        TrackId trackId,
        CatalogTrackRecordDto? existingRecord = null)
    {
        EnsureEmbeddedServerStarted();
        var store = EmbeddedServer.Instance.GetDocumentStore($"soundtrail-services-tests-{Guid.NewGuid():N}");

        if (existingRecord is not null)
        {
            using var session = store.OpenAsyncSession();
            await session.StoreAsync(existingRecord, existingRecord.Id);
            await session.SaveChangesAsync();
        }

        return new GetTrackPortContractTestEnvironment(
            new RavenGetTrackPort(store, new TypeRegistryFake()),
            trackId,
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
            var record = (CatalogTrackRecordDto)dto!;
            return new GetTrackResponse(
                TrackId.From(record.TrackId),
                new MusicCatalogId.Track(TrackId.From(record.TrackId)),
                record.Title,
                record.ArtistName,
                record.AlbumTitle,
                record.DurationMs,
                record.Isrc,
                record.ReleaseDate,
                record.ArtworkUrl);
        }

        public void MapOnto<TSource, TTarget>(TSource source, TTarget target)
            where TSource : class
            where TTarget : class => throw new NotSupportedException();
    }
}

public enum GetTrackPortImplementation
{
    Fake,
    Raven
}
