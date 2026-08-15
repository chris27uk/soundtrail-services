using Raven.Client.Documents;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;
using Soundtrail.Services.Tests.Integration.Shared.Infrastructure;
using Soundtrail.Services.Tests.Integration.Features.Search.Support;

namespace Soundtrail.Services.Tests.Integration.Features.Search;

internal sealed class SearchPortContractTestEnvironment : IAsyncDisposable
{
    private readonly IDocumentStore? documentStore;
    private readonly List<string> cleanupDocumentIds;

    private SearchPortContractTestEnvironment(
        ISearchPort subject,
        SearchCriteria searchCriteria,
        IDocumentStore? documentStore = null,
        List<string>? cleanupDocumentIds = null,
        string? seededCatalogItemId = null)
    {
        Subject = subject;
        SearchCriteria = searchCriteria;
        this.documentStore = documentStore;
        this.cleanupDocumentIds = cleanupDocumentIds ?? [];
        SeededCatalogItemId = seededCatalogItemId;
    }

    public ISearchPort Subject { get; }

    public SearchCriteria SearchCriteria { get; }

    public string? SeededCatalogItemId { get; }

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
        if (implementation == SearchPortImplementation.Fake)
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

            return new SearchPortContractTestEnvironment(
                new SearchPortFake(response),
                searchCriteria);
        }

        if (implementation != SearchPortImplementation.Raven)
        {
            throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null);
        }

        var isolation = EmbeddedRavenTestServer.NewIsolationKey();
        var uniqueQueryText = $"{queryText}-{isolation}";
        var uniqueMusicCatalogId = resultType == SearchType.Track
            ? global::Soundtrail.Services.Tests.TestTrackIds.Value($"track-search-{isolation}")
            : $"{musicCatalogId}-{isolation}";
        var ravenSearchCriteria = new SearchCriteria(uniqueQueryText, filter);

        return await CreateRavenEnvironmentAsync(
            ravenSearchCriteria,
            new CatalogSearchCandidateRecordDto
            {
                Id = CatalogSearchCandidateRecordDto.GetDocumentId(uniqueMusicCatalogId),
                CatalogItemId = uniqueMusicCatalogId,
                CandidateKind = resultType.ToString().ToLowerInvariant(),
                SearchText = uniqueQueryText,
                Title = title,
                ArtistName = artistName,
                AlbumTitle = albumTitle,
                ArtworkUrl = artworkUrl
            });
    }

    public static async Task<SearchPortContractTestEnvironment> ForMissingResults(
        SearchPortImplementation implementation,
        string queryText = "u2",
        SearchType filter = SearchType.Artist)
    {
        if (implementation == SearchPortImplementation.Fake)
        {
            var searchCriteria = new SearchCriteria(queryText, filter);
            return new SearchPortContractTestEnvironment(
                new SearchPortFake(),
                searchCriteria);
        }

        if (implementation != SearchPortImplementation.Raven)
        {
            throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null);
        }

        var isolation = EmbeddedRavenTestServer.NewIsolationKey();
        var ravenSearchCriteria = new SearchCriteria($"{queryText}-{isolation}", filter);
        return await CreateRavenEnvironmentAsync(ravenSearchCriteria);
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

    private static async Task<SearchPortContractTestEnvironment> CreateRavenEnvironmentAsync(
        SearchCriteria searchCriteria,
        CatalogSearchCandidateRecordDto? existingRecord = null)
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

        return new SearchPortContractTestEnvironment(
            new RavenSearchPort(store),
            searchCriteria,
            store,
            cleanupDocumentIds,
            existingRecord?.CatalogItemId);
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
