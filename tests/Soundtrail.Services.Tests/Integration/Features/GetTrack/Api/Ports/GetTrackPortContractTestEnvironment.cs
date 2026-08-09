using Raven.Client.Documents;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.Catalog.GetTrack.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTrack.Contract;
using Soundtrail.Services.Tests.Integration.Shared.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.GetTrack.Api.Ports;

internal sealed class GetTrackPortContractTestEnvironment : IAsyncDisposable
{
    private readonly IDocumentStore? documentStore;
    private readonly List<string> cleanupDocumentIds;

    private GetTrackPortContractTestEnvironment(
        IGetTrackPort subject,
        TrackId trackId,
        IDocumentStore? documentStore = null,
        List<string>? cleanupDocumentIds = null)
    {
        Subject = subject;
        TrackId = trackId;
        this.documentStore = documentStore;
        this.cleanupDocumentIds = cleanupDocumentIds ?? [];
    }

    public IGetTrackPort Subject { get; }

    public TrackId TrackId { get; }

    public static Task<GetTrackPortContractTestEnvironment> ForExistingTrack(
        GetTrackPortImplementation implementation,
        string? trackId = null,
        string musicCatalogId = "mc_track_601",
        string title = "The Track",
        string artistName = "The Artist",
        string? albumTitle = "The Album",
        int? durationMs = 201000,
        string? isrc = "GBAYE2400301",
        DateOnly? releaseDate = null,
        string? artworkUrl = "https://cdn.soundtrail.test/tracks/mc_track_601.jpg") =>
        implementation switch
        {
            GetTrackPortImplementation.Fake => Task.FromResult(CreateFakeExisting(
                trackId, musicCatalogId, title, artistName, albumTitle, durationMs, isrc, releaseDate, artworkUrl)),
            GetTrackPortImplementation.Raven => CreateRavenExistingAsync(
                musicCatalogId, title, artistName, albumTitle, durationMs, isrc, releaseDate, artworkUrl),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };

    public static Task<GetTrackPortContractTestEnvironment> ForMissingTrack(
        GetTrackPortImplementation implementation,
        TrackId? trackId = null) =>
        implementation switch
        {
            GetTrackPortImplementation.Fake => Task.FromResult(new GetTrackPortContractTestEnvironment(
                new GetTrackPortFake(),
                trackId ?? global::Soundtrail.Services.Tests.TestTrackIds.Create("track-602"))),
            GetTrackPortImplementation.Raven => CreateRavenEnvironmentAsync(
                global::Soundtrail.Services.Tests.TestTrackIds.Create(
                    $"track-602-{EmbeddedRavenTestServer.NewIsolationKey()}")),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };

    public async ValueTask DisposeAsync()
    {
        if (documentStore is null)
        {
            return;
        }

        await EmbeddedRavenTestServer.DeleteDocumentsAsync(documentStore, cleanupDocumentIds);
        await EmbeddedRavenTestServer.DisposeAsync(documentStore);
    }

    private static GetTrackPortContractTestEnvironment CreateFakeExisting(
        string? trackId,
        string musicCatalogId,
        string title,
        string artistName,
        string? albumTitle,
        int? durationMs,
        string? isrc,
        DateOnly? releaseDate,
        string? artworkUrl)
    {
        var trackIdValue = trackId ?? global::Soundtrail.Services.Tests.TestTrackIds.Value("track-601");
        var resolvedTrackId = TrackId.From(trackIdValue);
        var response = new GetTrackResponse(
            resolvedTrackId,
            title,
            artistName,
            albumTitle,
            durationMs,
            isrc,
            releaseDate ?? new DateOnly(2024, 1, 2),
            artworkUrl,
            false,
            []);

        return new GetTrackPortContractTestEnvironment(
            new GetTrackPortFake(response),
            resolvedTrackId);
    }

    private static async Task<GetTrackPortContractTestEnvironment> CreateRavenExistingAsync(
        string musicCatalogId,
        string title,
        string artistName,
        string? albumTitle,
        int? durationMs,
        string? isrc,
        DateOnly? releaseDate,
        string? artworkUrl)
    {
        var isolation = EmbeddedRavenTestServer.NewIsolationKey();
        var trackIdValue = global::Soundtrail.Services.Tests.TestTrackIds.Value($"track-601-{isolation}");
        return await CreateRavenEnvironmentAsync(
            TrackId.From(trackIdValue),
            new CatalogTrackRecordDto
            {
                Id = CatalogTrackRecordDto.GetDocumentId(trackIdValue),
                TrackId = trackIdValue,
                MusicCatalogId = $"{musicCatalogId}-{isolation}",
                Title = title,
                ArtistName = artistName,
                AlbumTitle = albumTitle,
                DurationMs = durationMs,
                Isrc = isrc,
                ReleaseDate = releaseDate ?? new DateOnly(2024, 1, 2),
                ArtworkUrl = artworkUrl
            });
    }

    private static async Task<GetTrackPortContractTestEnvironment> CreateRavenEnvironmentAsync(
        TrackId trackId,
        CatalogTrackRecordDto? existingRecord = null)
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

        return new GetTrackPortContractTestEnvironment(
            new RavenGetTrackPort(store, new TypeRegistryFake()),
            trackId,
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
            var record = (CatalogTrackRecordDto)dto!;
            return new GetTrackResponse(
                TrackId.From(record.TrackId),
                record.Title,
                record.ArtistName,
                record.AlbumTitle,
                record.DurationMs,
                record.Isrc,
                record.ReleaseDate,
                record.ArtworkUrl,
                record.StreamingLocations.Length > 0,
                record.StreamingLocations
                    .Select(static location => new Soundtrail.Services.Api.Shared.Contract.StreamingLocationResponse(
                        location.Provider,
                        location.ExternalId,
                        location.Url))
                    .ToArray());
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
