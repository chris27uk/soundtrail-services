using Raven.Client.Documents;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.GetAlbumsForArtist.Adapters;
using Soundtrail.Services.Api.Features.GetAlbumsForArtist.Contract;
using Soundtrail.Services.Tests.Integration.Ports;

namespace Soundtrail.Services.Tests.Integration.Ports.GetAlbumsForArtist;

internal sealed class GetAlbumsForArtistPortContractTestEnvironment : IAsyncDisposable
{
    private readonly IDocumentStore? documentStore;
    private readonly string? databaseName;

    private GetAlbumsForArtistPortContractTestEnvironment(
        IGetAlbumsForArtistPort subject,
        ArtistId artistId,
        IDocumentStore? documentStore = null,
        string? databaseName = null)
    {
        Subject = subject;
        ArtistId = artistId;
        this.documentStore = documentStore;
        this.databaseName = databaseName;
    }

    public IGetAlbumsForArtistPort Subject { get; }

    public ArtistId ArtistId { get; }

    public static async Task<GetAlbumsForArtistPortContractTestEnvironment> ForExistingArtistAlbums(
        GetAlbumsForArtistPortImplementation implementation,
        string artistId = "artist-2101",
        string artistName = "The Artist",
        string albumId = "album-2201",
        string musicCatalogId = "artist-2101:album-2201",
        string albumTitle = "The Album",
        DateOnly? releaseDate = null,
        string? artworkUrl = "https://cdn.soundtrail.test/albums/album-2201.jpg")
    {
        var resolvedArtistId = ArtistId.From(artistId);
        var resolvedAlbumId = AlbumId.From(artistId, albumId);
        var response = new GetAlbumsForArtistResponse(
            resolvedArtistId,
            ArtistName.From(artistName),
            [
                new GetAlbumsForArtistAlbumResponse(
                    resolvedAlbumId,
                    new CatalogItemId.Album(resolvedAlbumId),
                    albumTitle,
                    releaseDate ?? new DateOnly(2024, 1, 2),
                    artworkUrl)
            ]);

        return implementation switch
        {
            GetAlbumsForArtistPortImplementation.Fake => new GetAlbumsForArtistPortContractTestEnvironment(
                new GetAlbumsForArtistPortFake(response),
                resolvedArtistId),
            GetAlbumsForArtistPortImplementation.Raven => await CreateRavenEnvironmentAsync(
                resolvedArtistId,
                new CatalogArtistAlbumsRecordDto
                {
                    Id = CatalogArtistAlbumsRecordDto.GetDocumentId(artistId),
                    ArtistId = artistId,
                    ArtistName = artistName,
                    Albums =
                    [
                        new CatalogArtistAlbumRecordDto
                        {
                            AlbumId = albumId,
                            MusicCatalogId = musicCatalogId,
                            AlbumTitle = albumTitle,
                            ReleaseDate = response.Albums[0].ReleaseDate,
                            ArtworkUrl = artworkUrl
                        }
                    ]
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };
    }

    public static async Task<GetAlbumsForArtistPortContractTestEnvironment> ForMissingArtistAlbums(
        GetAlbumsForArtistPortImplementation implementation,
        ArtistId? artistId = null)
    {
        var resolvedArtistId = artistId ?? ArtistId.From("artist-2102");

        return implementation switch
        {
            GetAlbumsForArtistPortImplementation.Fake => new GetAlbumsForArtistPortContractTestEnvironment(
                new GetAlbumsForArtistPortFake(),
                resolvedArtistId),
            GetAlbumsForArtistPortImplementation.Raven => await CreateRavenEnvironmentAsync(resolvedArtistId),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };
    }

    public ValueTask DisposeAsync()
    {
        return EmbeddedRavenTestServer.DisposeAsync(documentStore, databaseName);
    }

    private static async Task<GetAlbumsForArtistPortContractTestEnvironment> CreateRavenEnvironmentAsync(
        ArtistId artistId,
        CatalogArtistAlbumsRecordDto? existingRecord = null)
    {
        var store = EmbeddedRavenTestServer.CreateDocumentStore();

        if (existingRecord is not null)
        {
            using var session = store.OpenAsyncSession();
            await session.StoreAsync(existingRecord, existingRecord.Id);
            await session.SaveChangesAsync();
        }

        return new GetAlbumsForArtistPortContractTestEnvironment(
            new RavenGetAlbumsForArtistPort(store, new TypeRegistryFake()),
            artistId,
            store,
            existingRecord?.Id);
    }

    private sealed class TypeRegistryFake : ITypeRegistry
    {
        public TDto ToDto<TDto>(object domainObject) where TDto : class => throw new NotSupportedException();

        public object ToDto(object domainObject) => throw new NotSupportedException();

        public TDomain ToDomainObject<TDomain>(object dto) where TDomain : class => (ToDomainObject(dto) as TDomain)!;

        public object ToDomainObject(object? dto)
        {
            var record = (CatalogArtistAlbumsRecordDto)dto!;
            return new GetAlbumsForArtistResponse(
                ArtistId.From(record.ArtistId),
                ArtistName.From(record.ArtistName),
                record.Albums.Select(
                        album => new GetAlbumsForArtistAlbumResponse(
                            AlbumId.From(record.ArtistId, album.AlbumId),
                            new CatalogItemId.Album(AlbumId.From(record.ArtistId, album.AlbumId)),
                            album.AlbumTitle,
                            album.ReleaseDate,
                            album.ArtworkUrl))
                    .ToArray());
        }

        public void MapOnto<TSource, TTarget>(TSource source, TTarget target)
            where TSource : class
            where TTarget : class => throw new NotSupportedException();
    }
}

public enum GetAlbumsForArtistPortImplementation
{
    Fake,
    Raven
}
