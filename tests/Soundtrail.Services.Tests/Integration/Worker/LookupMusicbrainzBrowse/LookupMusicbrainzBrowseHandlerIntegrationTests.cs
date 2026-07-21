using Microsoft.Extensions.Options;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzAlbumTracks;
using Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzArtistAlbums;
using Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzArtistTracks;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.MusicMetadata;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Soundtrail.Services.Tests.Integration.Worker.LookupMusicbrainzBrowse;

public sealed class LookupMusicbrainzBrowseHandlerIntegrationTests
{
    [Fact]
    public async Task Given_A_WireMock_Artist_Albums_Response_When_Handling_Then_A_Succeeded_Result_Is_Published()
    {
        using var environment = CreateEnvironment(
            """{"releases":[{"id":"release-mb-1","title":"Rare Release","date":"2026-01-02"}]}""",
            """{"recordings":[]}""",
            """{"title":"Rare Release","media":[]}""");
        var subject = new LookupMusicbrainzArtistAlbumsHandler(environment.Port, environment.Clock, environment.CommandBus);

        await subject.Handle(
            new LookupMusicbrainzArtistAlbumsMessage(
                MessageId.For("cmd-mb-artist-albums"),
                CorrelationId.From("corr:albums"),
                new DateTimeOffset(2026, 7, 20, 10, 30, 0, TimeSpan.Zero),
                LookupPriorityBand.High,
                ArtistId.From("artist-mb-1")),
            CancellationToken.None);

        environment.CommandBus.Messages.Single().Should().BeOfType<CatalogLookupCompleted>();
    }

    [Fact]
    public async Task Given_A_WireMock_Artist_Tracks_Response_When_Handling_Then_A_Succeeded_Result_Is_Published()
    {
        using var environment = CreateEnvironment(
            """{"releases":[]}""",
            """{"recordings":[{"id":"mbid-rare-1","title":"Rare Unknown Song","length":123000,"first-release-date":"2026-01-02","isrcs":["isrc-rare-1"],"artist-credit":[{"name":"Test Artist"}]}]}""",
            """{"title":"Rare Release","media":[]}""");
        var subject = new LookupMusicbrainzArtistTracksHandler(environment.Port, environment.Clock, environment.CommandBus);

        await subject.Handle(
            new LookupMusicbrainzArtistTracksMessage(
                MessageId.For("cmd-mb-artist-tracks"),
                CorrelationId.From("corr:tracks"),
                new DateTimeOffset(2026, 7, 20, 10, 30, 0, TimeSpan.Zero),
                LookupPriorityBand.High,
                ArtistId.From("artist-mb-1")),
            CancellationToken.None);

        environment.CommandBus.Messages.Single().Should().BeOfType<CatalogLookupCompleted>();
    }

    [Fact]
    public async Task Given_A_WireMock_Album_Tracks_Response_When_Handling_Then_A_Succeeded_Result_Is_Published()
    {
        using var environment = CreateEnvironment(
            """{"releases":[]}""",
            """{"recordings":[]}""",
            """{"title":"Rare Release","date":"2026-01-02","artist-credit":[{"name":"Test Artist"}],"media":[{"tracks":[{"title":"Album Track 1","length":123000,"artist-credit":[{"name":"Test Artist"}],"recording":{"id":"recording-mb-1","isrcs":["isrc-album-1"]}}]}]}""");
        var subject = new LookupMusicbrainzAlbumTracksHandler(environment.Port, environment.Clock, environment.CommandBus);

        await subject.Handle(
            new LookupMusicbrainzAlbumTracksMessage(
                MessageId.For("cmd-mb-album-tracks"),
                CorrelationId.From("corr:album-tracks"),
                new DateTimeOffset(2026, 7, 20, 10, 30, 0, TimeSpan.Zero),
                LookupPriorityBand.High,
                AlbumId.From("artist-mb-1", "release-mb-1")),
            CancellationToken.None);

        environment.CommandBus.Messages.Single().Should().BeOfType<CatalogLookupCompleted>();
    }

    private static Environment CreateEnvironment(string releaseBrowseJson, string recordingBrowseJson, string releaseLookupJson)
    {
        var server = WireMockServer.Start();
        server.Given(Request.Create().WithPath("/ws/2/release").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody(releaseBrowseJson));
        server.Given(Request.Create().WithPath("/ws/2/recording").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody(recordingBrowseJson));
        server.Given(Request.Create().WithPath("/ws/2/release/release-mb-1").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody(releaseLookupJson));

        var client = new HttpClient
        {
            BaseAddress = new Uri(server.Url!, UriKind.Absolute)
        };

        return new Environment(
            server,
            client,
            new MusicbrainzCatalogBrowsePort(
                client,
                Options.Create(new MusicBrainzOptions
                {
                    BaseUrl = server.Url!,
                    UserAgent = "Soundtrail.Tests/1.0"
                })),
            new ClockFake(),
            new CommandBusFake());
    }

    private sealed class Environment(
        WireMockServer server,
        HttpClient client,
        MusicbrainzCatalogBrowsePort port,
        ClockFake clock,
        CommandBusFake commandBus) : IDisposable
    {
        public MusicbrainzCatalogBrowsePort Port { get; } = port;
        public ClockFake Clock { get; } = clock;
        public CommandBusFake CommandBus { get; } = commandBus;

        public void Dispose()
        {
            client.Dispose();
            server.Dispose();
        }
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
