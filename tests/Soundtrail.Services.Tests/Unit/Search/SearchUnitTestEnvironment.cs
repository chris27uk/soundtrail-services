using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Catalog.Search;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;

namespace Soundtrail.Services.Tests.Unit.Search;

internal sealed class SearchUnitTestEnvironment
{
    private SearchUnitTestEnvironment(
        SearchRequest request,
        SearchPortFake port,
        DiscoveryFeedbackPortFake discoveryFeedbackPort,
        CommandBusFake commandBus,
        ClockPortFake clock)
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

    public ClockPortFake Clock { get; }

    public static SearchUnitTestEnvironment ForSearch(
        string queryText = "u2",
        SearchType filter = SearchType.Artist,
        SearchResponse? response = null) =>
        new(
            new SearchRequest(queryText, filter),
            new SearchPortFake(response ?? SearchResults.CreateResponse(queryText: queryText, filter: filter)),
            new DiscoveryFeedbackPortFake(),
            new CommandBusFake(),
            new ClockPortFake(new DateTimeOffset(2024, 6, 7, 8, 9, 10, TimeSpan.Zero)));

    public SearchHandler CreateSubjectUnderTest() => new(Port, CommandBus, DiscoveryFeedbackPort, Clock);

    public SearchRequest CreateRequest() => Request;

    public sealed class SearchPortFake(SearchResponse? response) : ISearchPort
    {
        public List<SearchCriteria> RequestedSearchCriteria { get; } = [];

        public Task<SearchResponse?> SearchAsync(SearchCriteria searchCriteria, CancellationToken cancellationToken)
        {
            RequestedSearchCriteria.Add(searchCriteria);
            return Task.FromResult(response);
        }
    }

    public sealed class CommandBusFake : ICommandBus
    {
        public List<RequestUnknownMusicDataCommand> Commands { get; } = [];

        public Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add((RequestUnknownMusicDataCommand)command);
            return Task.CompletedTask;
        }
    }

    public sealed class DiscoveryFeedbackPortFake : IDiscoveryFeedbackPort
    {
        public EnrichmentTarget? RequestedTarget { get; private set; }

        public DiscoveryFeedbackResponse? Response { get; set; }

        public Task<DiscoveryFeedbackResponse?> GetAsync(EnrichmentTarget target, CancellationToken cancellationToken)
        {
            RequestedTarget = target;
            return Task.FromResult(Response);
        }
    }

    public sealed class ClockPortFake(DateTimeOffset utcNow) : IClockPort
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
