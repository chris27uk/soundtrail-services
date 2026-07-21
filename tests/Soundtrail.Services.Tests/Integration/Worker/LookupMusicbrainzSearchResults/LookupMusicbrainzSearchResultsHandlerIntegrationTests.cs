using Microsoft.Extensions.Options;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzSearchResults;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.MusicMetadata;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Soundtrail.Services.Tests.Integration.Worker.LookupMusicbrainzSearchResults;

public sealed class LookupMusicbrainzSearchResultsHandlerIntegrationTests
{
    [Fact]
    public async Task Given_A_WireMock_Musicbrainz_Response_When_Handling_Then_A_Succeeded_Result_Is_Published()
    {
        using var server = WireMockServer.Start();
        server.Given(Request.Create().WithPath("/ws/2/artist").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
                .WithBody("""{"artists":[{"id":"artist-mb-1","name":"Test Artist"}]}"""));
        server.Given(Request.Create().WithPath("/ws/2/release").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
                .WithBody("""{"releases":[{"id":"release-mb-1","title":"Rare Release","date":"2026-01-02","artist-credit":[{"name":"Test Artist","artist":{"id":"artist-mb-1"}}]}]}"""));
        server.Given(Request.Create().WithPath("/ws/2/recording").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
                .WithBody("""{"recordings":[{"id":"mbid-rare-1","title":"Rare Unknown Song","length":123000,"first-release-date":"2026-01-02","isrcs":["isrc-rare-1"],"artist-credit":[{"name":"Test Artist","artist":{"id":"artist-mb-1"}}]}]}"""));

        using var client = new HttpClient
        {
            BaseAddress = new Uri(server.Url!, UriKind.Absolute)
        };

        var commandBus = new CommandBusFake();
        var handler = new LookupMusicbrainzSearchResultsHandler(
            new MusicbrainzCatalogSearchPort(
                client,
                Options.Create(new MusicBrainzOptions
                {
                    BaseUrl = server.Url!,
                    UserAgent = "Soundtrail.Tests/1.0"
                })),
            new ClockFake(),
            commandBus);
        var request = new LookupMusicbrainzSearchResultsMessage(
            MessageId.For("cmd-mb-search"),
            CorrelationId.From("corr:cmd-mb-search"),
            new DateTimeOffset(2026, 7, 20, 10, 30, 0, TimeSpan.Zero),
            LookupPriorityBand.High,
            new SearchCriteria("rare song", SearchType.All));

        await handler.Handle(request, CancellationToken.None);

        var completed = commandBus.Messages.Single().Should().BeOfType<CatalogLookupCompleted>().Subject;
        completed.Result.Should().BeOfType<LookupResult.Succeeded>();
    }

    private sealed class CommandBusFake : ICommandBus
    {
        public List<IMessage> Messages { get; } = [];

        public Task SendAsync(IMessage message, CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class ClockFake : IClockPort
    {
        public DateTimeOffset UtcNow => new(2026, 7, 20, 11, 45, 0, TimeSpan.Zero);
    }
}
