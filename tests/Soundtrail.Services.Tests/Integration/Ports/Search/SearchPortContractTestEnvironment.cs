using Raven.Client.Documents;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Search.Adapters;
using Soundtrail.Services.Api.Features.Search.Contract;
using Soundtrail.Services.Tests.Integration.Ports;

namespace Soundtrail.Services.Tests.Integration.Ports.Search;

internal sealed class SearchPortContractTestEnvironment : IAsyncDisposable
{
    private readonly IDocumentStore? documentStore;
    private readonly string? databaseName;

    private SearchPortContractTestEnvironment(
        ISearchPort subject,
        SearchCriteria searchCriteria,
        IDocumentStore? documentStore = null,
        string? databaseName = null)
    {
        Subject = subject;
        SearchCriteria = searchCriteria;
        this.documentStore = documentStore;
        this.databaseName = databaseName;
    }

    public ISearchPort Subject { get; }

    public SearchCriteria SearchCriteria { get; }

    public static async Task<SearchPortContractTestEnvironment> ForExistingResults(
        SearchPortImplementation implementation,
        string queryText = "u2",
        SearchFilter filter = SearchFilter.Artist,
        string musicCatalogId = "artist-3101",
        SearchFilter resultType = SearchFilter.Artist,
        string title = "U2",
        string? artistName = null,
        string? albumTitle = null,
        string? artworkUrl = "https://cdn.soundtrail.test/artists/artist-3101.jpg")
    {
        var searchCriteria = new SearchCriteria(queryText, filter);
        var response = new SearchResponse(
            queryText,
            filter,
            [
                new SearchResultResponse(
                    ParseMusicCatalogId(musicCatalogId, resultType),
                    resultType,
                    title,
                    artistName,
                    albumTitle,
                    artworkUrl)
            ]);

        return implementation switch
        {
            SearchPortImplementation.Fake => new SearchPortContractTestEnvironment(
                new SearchPortFake(response),
                searchCriteria),
            SearchPortImplementation.Raven => await CreateRavenEnvironmentAsync(
                searchCriteria,
                new CatalogSearchRecordDto
                {
                    Id = CatalogSearchRecordDto.GetDocumentId(searchCriteria.NormalisedIdentifier),
                    QueryText = queryText,
                    Filter = filter.ToString(),
                    Results =
                    [
                        new CatalogSearchResultRecordDto
                        {
                            MusicCatalogId = musicCatalogId,
                            ResultType = resultType.ToString(),
                            Title = title,
                            ArtistName = artistName,
                            AlbumTitle = albumTitle,
                            ArtworkUrl = artworkUrl
                        }
                    ]
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };
    }

    public static async Task<SearchPortContractTestEnvironment> ForMissingResults(
        SearchPortImplementation implementation,
        string queryText = "u2",
        SearchFilter filter = SearchFilter.Artist)
    {
        var searchCriteria = new SearchCriteria(queryText, filter);
        return implementation switch
        {
            SearchPortImplementation.Fake => new SearchPortContractTestEnvironment(
                new SearchPortFake(),
                searchCriteria),
            SearchPortImplementation.Raven => await CreateRavenEnvironmentAsync(searchCriteria),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };
    }

    public ValueTask DisposeAsync()
    {
        return EmbeddedRavenTestServer.DisposeAsync(documentStore, databaseName);
    }

    private static async Task<SearchPortContractTestEnvironment> CreateRavenEnvironmentAsync(
        SearchCriteria searchCriteria,
        CatalogSearchRecordDto? existingRecord = null)
    {
        var store = EmbeddedRavenTestServer.CreateDocumentStore();

        if (existingRecord is not null)
        {
            using var session = store.OpenAsyncSession();
            await session.StoreAsync(existingRecord, existingRecord.Id);
            await session.SaveChangesAsync();
        }

        return new SearchPortContractTestEnvironment(
            new RavenSearchPort(store, new TypeRegistryFake()),
            searchCriteria,
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
            var record = (CatalogSearchRecordDto)dto!;
            return new SearchResponse(
                record.QueryText,
                Enum.Parse<SearchFilter>(record.Filter, true),
                record.Results.Select(
                        result =>
                        {
                            var resultType = Enum.Parse<SearchFilter>(result.ResultType, true);
                            return new SearchResultResponse(
                                ParseMusicCatalogId(result.MusicCatalogId, resultType),
                                resultType,
                                result.Title,
                                result.ArtistName,
                                result.AlbumTitle,
                                result.ArtworkUrl);
                        })
                    .ToArray());
        }

        public void MapOnto<TSource, TTarget>(TSource source, TTarget target)
            where TSource : class
            where TTarget : class => throw new NotSupportedException();
    }

    private static CatalogItemId ParseMusicCatalogId(string value, SearchFilter filter) =>
        filter switch
        {
            SearchFilter.Artist => new CatalogItemId.Artist(ArtistId.From(value)),
            SearchFilter.Album => new CatalogItemId.Album(AlbumId.From(value)),
            SearchFilter.Track => new CatalogItemId.Track(TrackId.From(value)),
            _ => throw new InvalidOperationException($"Unsupported search filter '{filter}'.")
        };
}

public enum SearchPortImplementation
{
    Fake,
    Raven
}
