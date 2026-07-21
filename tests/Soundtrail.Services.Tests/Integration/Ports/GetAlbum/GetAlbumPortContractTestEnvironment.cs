using Raven.Client.Documents;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.GetAlbum.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetAlbum.Contract;
using Soundtrail.Services.Tests.Integration.Ports;

namespace Soundtrail.Services.Tests.Integration.Ports.GetAlbum;

internal sealed class GetAlbumPortContractTestEnvironment : IAsyncDisposable
{
    private readonly IDocumentStore? documentStore;
    private readonly string? databaseName;

    private GetAlbumPortContractTestEnvironment(
        IGetAlbumPort subject,
        AlbumId albumId,
        IDocumentStore? documentStore = null,
        string? databaseName = null)
    {
        Subject = subject;
        AlbumId = albumId;
        this.documentStore = documentStore;
        this.databaseName = databaseName;
    }

    public IGetAlbumPort Subject { get; }

    public AlbumId AlbumId { get; }

    public static async Task<GetAlbumPortContractTestEnvironment> ForExistingAlbum(
        GetAlbumPortImplementation implementation,
        string artistId = "artist-901",
        string albumId = "album-901",
        string artistName = "The Artist",
        string albumName = "The Album",
        DateOnly? releaseDate = null)
    {
        var resolvedAlbumId = AlbumId.From(artistId, albumId);
        var response = new GetAlbumResponse(
            ArtistId.From(artistId),
            ArtistName.From(artistName),
            resolvedAlbumId,
            albumName,
            releaseDate ?? new DateOnly(2024, 1, 2));

        return implementation switch
        {
            GetAlbumPortImplementation.Fake => new GetAlbumPortContractTestEnvironment(
                new GetAlbumPortFake(response),
                resolvedAlbumId),
            GetAlbumPortImplementation.Raven => await CreateRavenEnvironmentAsync(
                resolvedAlbumId,
                new CatalogAlbumRecordDto
                {
                    Id = CatalogAlbumRecordDto.GetDocumentId(resolvedAlbumId.ArtistAlbumId),
                    ArtistId = artistId,
                    AlbumId = albumId,
                    ArtistName = artistName,
                    Name = albumName,
                    ReleaseDate = response.ReleaseDate
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };
    }

    public static async Task<GetAlbumPortContractTestEnvironment> ForMissingAlbum(
        GetAlbumPortImplementation implementation,
        AlbumId? albumId = null)
    {
        var resolvedAlbumId = albumId ?? AlbumId.From("artist-902", "album-902");

        return implementation switch
        {
            GetAlbumPortImplementation.Fake => new GetAlbumPortContractTestEnvironment(
                new GetAlbumPortFake(),
                resolvedAlbumId),
            GetAlbumPortImplementation.Raven => await CreateRavenEnvironmentAsync(resolvedAlbumId),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };
    }

    public ValueTask DisposeAsync()
    {
        return EmbeddedRavenTestServer.DisposeAsync(documentStore, databaseName);
    }

    private static async Task<GetAlbumPortContractTestEnvironment> CreateRavenEnvironmentAsync(
        AlbumId albumId,
        CatalogAlbumRecordDto? existingRecord = null)
    {
        var store = EmbeddedRavenTestServer.CreateDocumentStore();

        if (existingRecord is not null)
        {
            using var session = store.OpenAsyncSession();
            await session.StoreAsync(existingRecord, existingRecord.Id);
            await session.SaveChangesAsync();
        }

        return new GetAlbumPortContractTestEnvironment(
            new RavenGetAlbumPort(store, new TypeRegistryFake()),
            albumId,
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
            var record = (CatalogAlbumRecordDto)dto!;
            return new GetAlbumResponse(
                ArtistId.From(record.ArtistId),
                ArtistName.From(record.ArtistName),
                AlbumId.From(record.ArtistId, record.AlbumId),
                record.Name,
                record.ReleaseDate);
        }

        public void MapOnto<TSource, TTarget>(TSource source, TTarget target)
            where TSource : class
            where TTarget : class => throw new NotSupportedException();
    }
}

public enum GetAlbumPortImplementation
{
    Fake,
    Raven
}
