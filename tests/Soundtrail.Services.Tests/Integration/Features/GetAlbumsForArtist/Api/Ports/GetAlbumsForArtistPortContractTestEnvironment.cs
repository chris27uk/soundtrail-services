using Raven.Client.Documents;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;
using Soundtrail.Services.Tests.Integration.Shared.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.GetAlbumsForArtist.Api.Ports;

internal sealed class GetAlbumsForArtistPortContractTestEnvironment : IAsyncDisposable
{
    private readonly IDocumentStore? documentStore;
    private readonly List<string> cleanupDocumentIds;

    private GetAlbumsForArtistPortContractTestEnvironment(
        IGetAlbumsForArtistPort subject,
        ArtistId artistId,
        IDocumentStore? documentStore = null,
        List<string>? cleanupDocumentIds = null)
    {
        Subject = subject;
        ArtistId = artistId;
        this.documentStore = documentStore;
        this.cleanupDocumentIds = cleanupDocumentIds ?? [];
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
        if (implementation == GetAlbumsForArtistPortImplementation.Fake)
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

            return new GetAlbumsForArtistPortContractTestEnvironment(
                new GetAlbumsForArtistPortFake(response),
                resolvedArtistId);
        }

        if (implementation != GetAlbumsForArtistPortImplementation.Raven)
        {
            throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null);
        }

        var isolation = EmbeddedRavenTestServer.NewIsolationKey();
        var uniqueArtistId = $"{artistId}-{isolation}";
        var uniqueAlbumId = $"{albumId}-{isolation}";
        var ravenArtistId = ArtistId.From(uniqueArtistId);
        var resolvedReleaseDate = releaseDate ?? new DateOnly(2024, 1, 2);

        return await CreateRavenEnvironmentAsync(
            ravenArtistId,
            new CatalogArtistAlbumsRecordDto
            {
                Id = CatalogArtistAlbumsRecordDto.GetDocumentId(uniqueArtistId),
                ArtistId = uniqueArtistId,
                ArtistName = artistName,
                Albums =
                [
                    new CatalogArtistAlbumRecordDto
                    {
                        AlbumId = uniqueAlbumId,
                        MusicCatalogId = $"{musicCatalogId}-{isolation}",
                        AlbumTitle = albumTitle,
                        ReleaseDate = resolvedReleaseDate,
                        ArtworkUrl = artworkUrl
                    }
                ]
            });
    }

    public static async Task<GetAlbumsForArtistPortContractTestEnvironment> ForMissingArtistAlbums(
        GetAlbumsForArtistPortImplementation implementation,
        ArtistId? artistId = null)
    {
        if (implementation == GetAlbumsForArtistPortImplementation.Fake)
        {
            var resolvedArtistId = artistId ?? ArtistId.From("artist-2102");
            return new GetAlbumsForArtistPortContractTestEnvironment(
                new GetAlbumsForArtistPortFake(),
                resolvedArtistId);
        }

        if (implementation != GetAlbumsForArtistPortImplementation.Raven)
        {
            throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null);
        }

        var isolation = EmbeddedRavenTestServer.NewIsolationKey();
        var ravenArtistId = ArtistId.From($"artist-2102-{isolation}");
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

    private static async Task<GetAlbumsForArtistPortContractTestEnvironment> CreateRavenEnvironmentAsync(
        ArtistId artistId,
        CatalogArtistAlbumsRecordDto? existingRecord = null)
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

        return new GetAlbumsForArtistPortContractTestEnvironment(
            new RavenGetAlbumsForArtistPort(store, new TypeRegistryFake()),
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
