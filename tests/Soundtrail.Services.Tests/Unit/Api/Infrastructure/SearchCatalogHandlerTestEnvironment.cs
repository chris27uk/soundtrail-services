using Soundtrail.Domain;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Search;
using Soundtrail.Services.Api.Features.Search.Adapters;
using Soundtrail.Services.Tests.Integration.Api.Features.Search;

namespace Soundtrail.Services.Tests.Unit.Api.Infrastructure;

internal sealed class SearchCatalogHandlerTestEnvironment
{
    private SearchCatalogHandlerTestEnvironment(
        FakeCatalogSearchPort catalogSearch,
        InMemoryEnqueueMusicRequest enqueueMusicRequests)
    {
        CatalogSearch = catalogSearch;
        EnqueueMusicRequests = enqueueMusicRequests;
        Handler = new SearchCatalogHandler(catalogSearch, enqueueMusicRequests);
    }

    public IHandler<SearchCatalogCommand, SearchCatalogResponse> Handler { get; }

    public FakeCatalogSearchPort CatalogSearch { get; }

    public InMemoryEnqueueMusicRequest EnqueueMusicRequests { get; }

    public static SearchCatalogHandlerTestEnvironment WithKnownTrack() =>
        new(
            new FakeCatalogSearchPort([ApiKnownTracks.MrBrightsideCatalogTrack()]),
            new InMemoryEnqueueMusicRequest());

    public static SearchCatalogHandlerTestEnvironment WithNoKnownResults() =>
        new(
            new FakeCatalogSearchPort([], discovery: null, isComplete: false),
            new InMemoryEnqueueMusicRequest());

    public static SearchCatalogHandlerTestEnvironment WithPendingDiscovery() =>
        new(
            new FakeCatalogSearchPort(
                [],
                new SearchDiscovery(true, "Already planned", 30),
                isComplete: false),
            new InMemoryEnqueueMusicRequest());

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
}
