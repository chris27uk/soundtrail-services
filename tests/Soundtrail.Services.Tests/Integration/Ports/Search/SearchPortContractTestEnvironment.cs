using Raven.Client.Documents;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;
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
        SearchType filter = SearchType.Artist,
        string musicCatalogId = "artist-3101",
        SearchType resultType = SearchType.Artist,
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
                new CatalogSearchCandidateRecordDto
                {
                    Id = CatalogSearchCandidateRecordDto.GetDocumentId(musicCatalogId),
                    CatalogItemId = musicCatalogId,
                    CandidateKind = resultType.ToString().ToLowerInvariant(),
                    SearchText = queryText,
                    Title = title,
                    ArtistName = artistName,
                    AlbumTitle = albumTitle,
                    ArtworkUrl = artworkUrl
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };
    }

    public static async Task<SearchPortContractTestEnvironment> ForMissingResults(
        SearchPortImplementation implementation,
        string queryText = "u2",
        SearchType filter = SearchType.Artist)
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
        CatalogSearchCandidateRecordDto? existingRecord = null)
    {
        var store = EmbeddedRavenTestServer.CreateDocumentStore();

        if (existingRecord is not null)
        {
            using var session = store.OpenAsyncSession();
            await session.StoreAsync(existingRecord, existingRecord.Id);
            await session.SaveChangesAsync();
        }

        return new SearchPortContractTestEnvironment(
            new RavenSearchPort(store),
            searchCriteria,
            store,
            existingRecord?.Id);
    }

    private static CatalogItemId ParseMusicCatalogId(string value, SearchType filter) =>
        filter switch
        {
            SearchType.Artist => new CatalogItemId.Artist(ArtistId.From(value)),
            SearchType.Album => new CatalogItemId.Album(AlbumId.From(value)),
            SearchType.Track => new CatalogItemId.Track(TrackId.From(value)),
            _ => throw new InvalidOperationException($"Unsupported search filter '{filter}'.")
        };
}

public enum SearchPortImplementation
{
    Fake,
    Raven
}
