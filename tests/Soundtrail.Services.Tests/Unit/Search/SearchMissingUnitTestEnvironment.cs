using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Catalog.Search;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;

namespace Soundtrail.Services.Tests.Unit.Search;

internal sealed class SearchMissingUnitTestEnvironment
{
    private SearchMissingUnitTestEnvironment(
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

    public static SearchMissingUnitTestEnvironment ForMissingSearch(
        string queryText = "u2",
        SearchType filter = SearchType.Artist) =>
        new(
            new SearchRequest(queryText, filter),
            new SearchPortFake(),
            new DiscoveryFeedbackPortFake(),
            new CommandBusFake(),
            new ClockPortFake(new DateTimeOffset(2024, 6, 7, 8, 9, 10, TimeSpan.Zero)));

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

    public sealed class CommandBusFake : ICommandBus
    {
        public List<RequestUnknownMusicDataMessage> Commands { get; } = [];

        public Task SendAsync(IMessage message, CancellationToken cancellationToken = default)
        {
            Commands.Add((RequestUnknownMusicDataMessage)message);
            return Task.CompletedTask;
        }
    }

    public sealed class DiscoveryFeedbackPortFake : IDiscoveryFeedbackPort
    {
        public Task<DiscoveryFeedbackResponse?> GetAsync(EnrichmentTarget target, CancellationToken cancellationToken) =>
            Task.FromResult<DiscoveryFeedbackResponse?>(null);
    }

    public sealed class ClockPortFake(DateTimeOffset utcNow) : IClockPort
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
