using Raven.Client.Documents;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.GetArtist.Adapters;
using Soundtrail.Services.Api.Features.GetArtist.Contract;
using Soundtrail.Services.Tests.Integration.Ports;

namespace Soundtrail.Services.Tests.Integration.Ports.GetArtist;

internal sealed class GetArtistPortContractTestEnvironment : IAsyncDisposable
{
    private readonly IDocumentStore? documentStore;
    private readonly string? databaseName;

    private GetArtistPortContractTestEnvironment(
        IGetArtistPort subject,
        ArtistId artistId,
        IDocumentStore? documentStore = null,
        string? databaseName = null)
    {
        Subject = subject;
        ArtistId = artistId;
        this.documentStore = documentStore;
        this.databaseName = databaseName;
    }

    public IGetArtistPort Subject { get; }

    public ArtistId ArtistId { get; }

    public static async Task<GetArtistPortContractTestEnvironment> ForExistingArtist(
        GetArtistPortImplementation implementation,
        string artistId = "artist-1001",
        string artistName = "The Artist",
        string? imageUrl = "https://cdn.soundtrail.test/artists/artist-1001.jpg")
    {
        var resolvedArtistId = ArtistId.From(artistId);
        var response = new GetArtistResponse(
            resolvedArtistId,
            ArtistName.From(artistName),
            null,
            imageUrl);

        return implementation switch
        {
            GetArtistPortImplementation.Fake => new GetArtistPortContractTestEnvironment(
                new GetArtistPortFake(response),
                resolvedArtistId),
            GetArtistPortImplementation.Raven => await CreateRavenEnvironmentAsync(
                resolvedArtistId,
                new CatalogArtistRecordDto
                {
                    Id = CatalogArtistRecordDto.GetDocumentId(artistId),
                    ArtistId = artistId,
                    Name = artistName,
                    ArtworkUrl = imageUrl
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };
    }

    public static async Task<GetArtistPortContractTestEnvironment> ForMissingArtist(
        GetArtistPortImplementation implementation,
        ArtistId? artistId = null)
    {
        var resolvedArtistId = artistId ?? ArtistId.From("artist-1002");

        return implementation switch
        {
            GetArtistPortImplementation.Fake => new GetArtistPortContractTestEnvironment(
                new GetArtistPortFake(),
                resolvedArtistId),
            GetArtistPortImplementation.Raven => await CreateRavenEnvironmentAsync(resolvedArtistId),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };
    }

    public ValueTask DisposeAsync()
    {
        return EmbeddedRavenTestServer.DisposeAsync(documentStore, databaseName);
    }

    private static async Task<GetArtistPortContractTestEnvironment> CreateRavenEnvironmentAsync(
        ArtistId artistId,
        CatalogArtistRecordDto? existingRecord = null)
    {
        var store = EmbeddedRavenTestServer.CreateDocumentStore();

        if (existingRecord is not null)
        {
            using var session = store.OpenAsyncSession();
            await session.StoreAsync(existingRecord, existingRecord.Id);
            await session.SaveChangesAsync();
        }

        return new GetArtistPortContractTestEnvironment(
            new RavenGetArtistPort(store, new TypeRegistryFake()),
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
