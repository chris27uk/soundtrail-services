using Raven.Client.Documents;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.GetArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetArtist.Contract;
using Soundtrail.Services.Tests.Integration.Shared.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.GetArtist.Api.Ports;

internal sealed class GetArtistPortContractTestEnvironment : IAsyncDisposable
{
    private readonly IDocumentStore? documentStore;
    private readonly List<string> cleanupDocumentIds;

    private GetArtistPortContractTestEnvironment(
        IGetArtistPort subject,
        ArtistId artistId,
        IDocumentStore? documentStore = null,
        List<string>? cleanupDocumentIds = null)
    {
        Subject = subject;
        ArtistId = artistId;
        this.documentStore = documentStore;
        this.cleanupDocumentIds = cleanupDocumentIds ?? [];
    }

    public IGetArtistPort Subject { get; }

    public ArtistId ArtistId { get; }

    public static async Task<GetArtistPortContractTestEnvironment> ForExistingArtist(
        GetArtistPortImplementation implementation,
        string artistId = "artist-1001",
        string artistName = "The Artist",
        string? imageUrl = "https://cdn.soundtrail.test/artists/artist-1001.jpg")
    {
        if (implementation == GetArtistPortImplementation.Fake)
        {
            var resolvedArtistId = ArtistId.From(artistId);
            var response = new GetArtistResponse(
                resolvedArtistId,
                ArtistName.From(artistName),
                null,
                imageUrl);

            return new GetArtistPortContractTestEnvironment(
                new GetArtistPortFake(response),
                resolvedArtistId);
        }

        if (implementation != GetArtistPortImplementation.Raven)
        {
            throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null);
        }

        var isolation = EmbeddedRavenTestServer.NewIsolationKey();
        var uniqueArtistId = $"{artistId}-{isolation}";
        var ravenArtistId = ArtistId.From(uniqueArtistId);

        return await CreateRavenEnvironmentAsync(
            ravenArtistId,
            new CatalogArtistRecordDto
            {
                Id = CatalogArtistRecordDto.GetDocumentId(uniqueArtistId),
                ArtistId = uniqueArtistId,
                Name = artistName,
                ArtworkUrl = imageUrl
            });
    }

    public static async Task<GetArtistPortContractTestEnvironment> ForMissingArtist(
        GetArtistPortImplementation implementation,
        ArtistId? artistId = null)
    {
        if (implementation == GetArtistPortImplementation.Fake)
        {
            var resolvedArtistId = artistId ?? ArtistId.From("artist-1002");
            return new GetArtistPortContractTestEnvironment(
                new GetArtistPortFake(),
                resolvedArtistId);
        }

        if (implementation != GetArtistPortImplementation.Raven)
        {
            throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null);
        }

        var isolation = EmbeddedRavenTestServer.NewIsolationKey();
        var ravenArtistId = ArtistId.From($"artist-1002-{isolation}");
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

    private static async Task<GetArtistPortContractTestEnvironment> CreateRavenEnvironmentAsync(
        ArtistId artistId,
        CatalogArtistRecordDto? existingRecord = null)
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

        return new GetArtistPortContractTestEnvironment(
            new RavenGetArtistPort(store, new TypeRegistryFake()),
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
            var record = (CatalogArtistRecordDto)dto!;
            return new GetArtistResponse(
                ArtistId.From(record.ArtistId),
                ArtistName.From(record.Name),
                null,
                record.ArtworkUrl);
        }

        public void MapOnto<TSource, TTarget>(TSource source, TTarget target)
            where TSource : class
            where TTarget : class => throw new NotSupportedException();
    }
}

public enum GetArtistPortImplementation
{
    Fake,
    Raven
}
