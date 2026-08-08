using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Catalog.Search;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;
using Soundtrail.Services.Tests.Fakes;

namespace Soundtrail.Services.Tests.Unit.Search;

internal sealed class SearchMissingUnitTestEnvironment
{
    private SearchMissingUnitTestEnvironment(
        SearchRequest request,
        SearchPortFake port,
        DiscoveryFeedbackPortFake discoveryFeedbackPort,
        CommandBusFake commandBus,
        ClockFake clock)
    {
        Request = request;
        Port = port;
        DiscoveryFeedbackPort = discoveryFeedbackPort;
        CommandBus = commandBus;
        Clock = clock;
    }

    public SearchRequest Request { get; }

    public SearchPortFake Port { get; }

    public DiscoveryFeedbackPortFake DiscoveryFeedbackPort { get; }

    public CommandBusFake CommandBus { get; }

    public ClockFake Clock { get; }

    public static SearchMissingUnitTestEnvironment ForMissingSearch(
        string queryText = "u2",
        SearchType filter = SearchType.Artist) =>
        new(
            new SearchRequest(queryText, filter),
            new SearchPortFake(),
            new DiscoveryFeedbackPortFake(),
            new CommandBusFake(),
            new ClockFake(new DateTimeOffset(2024, 6, 7, 8, 9, 10, TimeSpan.Zero)));

    public SearchHandler CreateSubjectUnderTest() => new(Port, CommandBus, DiscoveryFeedbackPort, Clock);

    public SearchRequest CreateRequest() => Request;

    public sealed class SearchPortFake : ISearchPort
    {
        public List<SearchCriteria> RequestedSearchCriteria { get; } = [];

        public Task<SearchResponse?> SearchAsync(SearchCriteria searchCriteria, CancellationToken cancellationToken)
        {
            RequestedSearchCriteria.Add(searchCriteria);
            return Task.FromResult<SearchResponse?>(null);
        }
    }

    public sealed class DiscoveryFeedbackPortFake : IDiscoveryFeedbackPort
    {
        public Task<DiscoveryFeedbackResponse?> GetAsync(EnrichmentTarget target, CancellationToken cancellationToken) =>
            Task.FromResult<DiscoveryFeedbackResponse?>(null);
    }
}
