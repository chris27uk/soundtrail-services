using Soundtrail.Domain;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Search.SearchCatalog;
using Soundtrail.Services.Api.Features.Search.SearchCatalog.Adapters;
using Soundtrail.Services.Tests.Integration.Api.Features.Search;

namespace Soundtrail.Services.Tests.Unit.Api.Infrastructure;

internal sealed class SearchCatalogHandlerTestEnvironment
{
    private SearchCatalogHandlerTestEnvironment(
        FakeCatalogSearchPort catalogSearch,
        InMemoryEnqueueCatalogSearchAttempt enqueueMusicRequests,
        InMemoryRecordCatalogSearchAttempt requestDiscovery)
    {
        CatalogSearch = catalogSearch;
        CatalogSearchAttemptQueues = enqueueMusicRequests;
        RecordedCatalogSearchAttempts = requestDiscovery;
        Handler = new SearchCatalogHandler(catalogSearch, requestDiscovery);
    }

    public IHandler<SearchCatalogCommand, SearchCatalogResponse> Handler { get; }

    public FakeCatalogSearchPort CatalogSearch { get; }

    public InMemoryEnqueueCatalogSearchAttempt CatalogSearchAttemptQueues { get; }

    public InMemoryRecordCatalogSearchAttempt RecordedCatalogSearchAttempts { get; }

    public static SearchCatalogHandlerTestEnvironment WithKnownTrack() =>
        Create(
            new FakeCatalogSearchPort([ApiKnownTracks.MrBrightsideCatalogTrack()]));

    public static SearchCatalogHandlerTestEnvironment WithNoKnownResults() =>
        Create(
            new FakeCatalogSearchPort([], discovery: null, isComplete: false));

    public static SearchCatalogHandlerTestEnvironment WithPendingDiscovery() =>
        Create(
            new FakeCatalogSearchPort(
                [],
                new SearchDiscovery(true, "Already planned", 30),
                isComplete: false));

    public static SearchCatalogHandlerTestEnvironment WithRecordedDiscoveryRequest() =>
        Create(
            new FakeCatalogSearchPort([], discovery: null, isComplete: false),
            seedRecordedCatalogSearchAttempts: true);

    public SearchCatalogCommand Request(
        string query,
        string? types = null,
        string? playback = null,
        int? limit = null,
        int? offset = null) =>
        new(
            NormalizedSearchQuery.FromText(query),
            SearchTypesFilter.Parse(types),
            PlaybackProviderFilter.Parse(playback),
            SearchLimit.From(limit),
            SearchOffset.From(offset));

    private static SearchCatalogHandlerTestEnvironment Create(
        FakeCatalogSearchPort catalogSearch,
        bool seedRecordedCatalogSearchAttempts = false)
    {
        var queue = new InMemoryEnqueueCatalogSearchAttempt();
        var requestDiscovery = new InMemoryRecordCatalogSearchAttempt(queue);
        var env = new SearchCatalogHandlerTestEnvironment(catalogSearch, queue, requestDiscovery);

        if (seedRecordedCatalogSearchAttempts)
        {
            requestDiscovery.Seed(env.Request("rare unknown song").ToCatalogSearchCriteria());
        }

        return env;
    }
}
