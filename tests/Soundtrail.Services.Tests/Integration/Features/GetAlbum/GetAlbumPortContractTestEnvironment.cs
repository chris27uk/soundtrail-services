using Raven.Client.Documents;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.GetAlbum.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetAlbum.Contract;
using Soundtrail.Services.Tests.Integration.Shared.Infrastructure;
using Soundtrail.Services.Tests.Integration.Features.GetAlbum.Support;

namespace Soundtrail.Services.Tests.Integration.Features.GetAlbum;

internal sealed class GetAlbumPortContractTestEnvironment : IAsyncDisposable
{
    private readonly IDocumentStore? documentStore;
    private readonly List<string> cleanupDocumentIds;

    private GetAlbumPortContractTestEnvironment(
        IGetAlbumPort subject,
        AlbumId albumId,
        IDocumentStore? documentStore = null,
        List<string>? cleanupDocumentIds = null)
    {
        Subject = subject;
        AlbumId = albumId;
        this.documentStore = documentStore;
        this.cleanupDocumentIds = cleanupDocumentIds ?? [];
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
        if (implementation == GetAlbumPortImplementation.Fake)
        {
            var resolvedAlbumId = AlbumId.From(artistId, albumId);
            var response = new GetAlbumResponse(
                ArtistId.From(artistId),
                ArtistName.From(artistName),
                resolvedAlbumId,
                albumName,
                releaseDate ?? new DateOnly(2024, 1, 2));

            return new GetAlbumPortContractTestEnvironment(
                new GetAlbumPortFake(response),
                resolvedAlbumId);
        }

        if (implementation != GetAlbumPortImplementation.Raven)
        {
            throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null);
        }

        var isolation = EmbeddedRavenTestServer.NewIsolationKey();
        var uniqueArtistId = $"{artistId}-{isolation}";
        var uniqueAlbumId = $"{albumId}-{isolation}";
        var ravenAlbumId = AlbumId.From(uniqueArtistId, uniqueAlbumId);
        var resolvedReleaseDate = releaseDate ?? new DateOnly(2024, 1, 2);

        return await CreateRavenEnvironmentAsync(
            ravenAlbumId,
            new CatalogAlbumRecordDto
            {
                Id = CatalogAlbumRecordDto.GetDocumentId(ravenAlbumId.ArtistAlbumId),
                ArtistId = uniqueArtistId,
                AlbumId = uniqueAlbumId,
                ArtistName = artistName,
                Name = albumName,
                ReleaseDate = resolvedReleaseDate
            });
    }

    public static async Task<GetAlbumPortContractTestEnvironment> ForMissingAlbum(
        GetAlbumPortImplementation implementation,
        AlbumId? albumId = null)
    {
        if (implementation == GetAlbumPortImplementation.Fake)
        {
            var resolvedAlbumId = albumId ?? AlbumId.From("artist-902", "album-902");
            return new GetAlbumPortContractTestEnvironment(
                new GetAlbumPortFake(),
                resolvedAlbumId);
        }

        if (implementation != GetAlbumPortImplementation.Raven)
        {
            throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null);
        }

        var isolation = EmbeddedRavenTestServer.NewIsolationKey();
        var ravenAlbumId = AlbumId.From($"artist-902-{isolation}", $"album-902-{isolation}");
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

    private static async Task<GetAlbumPortContractTestEnvironment> CreateRavenEnvironmentAsync(
        AlbumId albumId,
        CatalogAlbumRecordDto? existingRecord = null)
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

        return new GetAlbumPortContractTestEnvironment(
            new RavenGetAlbumPort(store, new TypeRegistryFake()),
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
