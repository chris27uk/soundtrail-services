using Raven.Client.Documents;
using Raven.Embedded;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Api.Features.GetAlbum.Adapters;
using Soundtrail.Services.Api.Features.GetAlbum.Contract;

namespace Soundtrail.Services.Tests.Integration.Ports.GetAlbum;

internal sealed class GetAlbumPortContractTestEnvironment : IAsyncDisposable
{
    private static int serverStarted;
    private readonly IDocumentStore? documentStore;

    private GetAlbumPortContractTestEnvironment(
        IGetAlbumPort subject,
        AlbumId albumId,
        IDocumentStore? documentStore = null)
    {
        Subject = subject;
        AlbumId = albumId;
        this.documentStore = documentStore;
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
        documentStore?.Dispose();
        return ValueTask.CompletedTask;
    }

    private static async Task<GetAlbumPortContractTestEnvironment> CreateRavenEnvironmentAsync(
        AlbumId albumId,
        CatalogAlbumRecordDto? existingRecord = null)
    {
        EnsureEmbeddedServerStarted();
        var store = EmbeddedServer.Instance.GetDocumentStore($"soundtrail-services-tests-{Guid.NewGuid():N}");

        if (existingRecord is not null)
        {
            using var session = store.OpenAsyncSession();
            await session.StoreAsync(existingRecord, existingRecord.Id);
            await session.SaveChangesAsync();
        }

        return new GetAlbumPortContractTestEnvironment(
            new RavenGetAlbumPort(store, new TypeRegistryFake()),
            albumId,
            store);
    }

    private static void EnsureEmbeddedServerStarted()
    {
        if (Interlocked.Exchange(ref serverStarted, 1) == 1)
        {
            return;
        }

        EmbeddedServer.Instance.StartServer();
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
