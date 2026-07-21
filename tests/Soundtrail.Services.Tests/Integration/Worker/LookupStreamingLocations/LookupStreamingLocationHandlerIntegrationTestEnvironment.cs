using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Features.LookupStreamingLocationByIsrc;
using Soundtrail.Services.Enrichment.Worker.Features.LookupStreamingLocationByTrackMetadata;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.StreamingLocations;
using Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;
using Soundtrail.Services.Tests.Integration.Ports;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Soundtrail.Services.Tests.Integration.Worker.LookupStreamingLocations;

internal sealed class LookupStreamingLocationHandlerIntegrationTestEnvironment : IDisposable
{
    private readonly WireMockServer wireMockServer;
    private readonly HttpClient httpClient;
    private readonly IDocumentStore documentStore;
    private readonly List<string> cleanupDocumentIds = [];

    private LookupStreamingLocationHandlerIntegrationTestEnvironment(
        IDocumentStore documentStore,
        WireMockServer wireMockServer,
        HttpClient httpClient,
        CommandBusFake commandBus,
        ClockFake clock)
    {
        this.documentStore = documentStore;
        this.wireMockServer = wireMockServer;
        this.httpClient = httpClient;
        CommandBus = commandBus;
        Clock = clock;
    }

    public CommandBusFake CommandBus { get; }

    public ClockFake Clock { get; }

    public static async Task<LookupStreamingLocationHandlerIntegrationTestEnvironment> CreateAsync(
        string responseJson,
        string seed = "streaming-integration-track",
        string title = "Road Song",
        string artistName = "The Travellers",
        string? isrc = "GBAYE2410001")
    {
        var store = EmbeddedRavenTestServer.CreateDocumentStore();
        var server = WireMockServer.Start();
        server
            .Given(Request.Create().WithPath("/v1-user/links").UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(responseJson));

        var client = new HttpClient
        {
            BaseAddress = new Uri(server.Url!, UriKind.Absolute)
        };

        var environment = new LookupStreamingLocationHandlerIntegrationTestEnvironment(
            store,
            server,
            client,
            new CommandBusFake(),
            new ClockFake());

        await environment.SeedTrackAsync(seed, title, artistName, isrc);
        return environment;
    }

    public LookupStreamingLocationByIsrcHandler CreateIsrcSubject() =>
        new(
            new RavenReadTrackForLookupPort(documentStore),
            new OdesliStreamingLocationPort(
                httpClient,
                Options.Create(new OdesliOptions
                {
                    BaseUrl = wireMockServer.Url!,
                    UserCountry = "US"
                })),
            Clock,
            CommandBus);

    public LookupStreamingLocationByTrackMetadataHandler CreateMetadataSubject() =>
        new(
            new RavenReadTrackForLookupPort(documentStore),
            new OdesliStreamingLocationPort(
                httpClient,
                Options.Create(new OdesliOptions
                {
                    BaseUrl = wireMockServer.Url!,
                    UserCountry = "US"
                })),
            Clock,
            CommandBus);

    public LookupStreamingLocationByIsrcMessage CreateIsrcRequest(string seed = "streaming-integration-track") =>
        new(
            MessageId.For($"cmd-isrc:{seed}"),
            CorrelationId.From($"corr:{seed}"),
            new DateTimeOffset(2026, 7, 20, 10, 30, 0, TimeSpan.Zero),
            LookupPriorityBand.High,
            global::Soundtrail.Services.Tests.TestTrackIds.Create(seed),
            ProviderName.Spotify);

    public LookupStreamingLocationByTrackMetadataMessage CreateMetadataRequest(string seed = "streaming-integration-track") =>
        new(
            MessageId.For($"cmd-metadata:{seed}"),
            CorrelationId.From($"corr:{seed}"),
            new DateTimeOffset(2026, 7, 20, 10, 30, 0, TimeSpan.Zero),
            LookupPriorityBand.High,
            global::Soundtrail.Services.Tests.TestTrackIds.Create(seed),
            ProviderName.AppleMusic);

    public void Dispose()
    {
        httpClient.Dispose();
        wireMockServer.Dispose();

        foreach (var documentId in cleanupDocumentIds)
        {
            EmbeddedRavenTestServer.DisposeAsync(documentStore, documentId).AsTask().GetAwaiter().GetResult();
        }
    }

    private async Task SeedTrackAsync(string seed, string title, string artistName, string? isrc)
    {
        var trackId = global::Soundtrail.Services.Tests.TestTrackIds.Create(seed);
        var artistId = ArtistId.From($"artist-{Guid.NewGuid():N}");
        var trackDocumentId = CatalogTrackRecordDto.GetDocumentId(trackId.Value);
        var artistTracksDocumentId = CatalogArtistTracksRecordDto.GetDocumentId(artistId.Value);

        cleanupDocumentIds.Add(trackDocumentId);
        cleanupDocumentIds.Add(artistTracksDocumentId);

        using var session = documentStore.OpenAsyncSession();
        await session.StoreAsync(
            new CatalogTrackRecordDto
            {
                Id = trackDocumentId,
                TrackId = trackId.Value,
                TrackIdBaseKeyHigh = trackId.BaseKeyHigh,
                TrackIdBaseKeyLow = trackId.BaseKeyLow,
                TrackIdSpecificKey = trackId.SpecificKey,
                MusicCatalogId = trackId.Value,
                Title = title,
                ArtistName = artistName,
                Isrc = isrc,
                UpdatedAt = new DateTimeOffset(2026, 7, 20, 11, 0, 0, TimeSpan.Zero)
            });
        await session.StoreAsync(
            new CatalogArtistTracksRecordDto
            {
                Id = artistTracksDocumentId,
                ArtistId = artistId.Value,
                ArtistName = artistName,
                Tracks =
                [
                    new CatalogArtistTrackRecordDto
                    {
                        TrackId = trackId.Value,
                        TrackIdBaseKeyHigh = trackId.BaseKeyHigh,
                        TrackIdBaseKeyLow = trackId.BaseKeyLow,
                        TrackIdSpecificKey = trackId.SpecificKey,
                        MusicCatalogId = trackId.Value,
                        Title = title,
                        ArtistName = artistName,
                        Isrc = isrc
                    }
                ],
                UpdatedAt = new DateTimeOffset(2026, 7, 20, 11, 0, 0, TimeSpan.Zero)
            });
        await session.SaveChangesAsync();
    }

    public sealed class CommandBusFake : ICommandBus
    {
        public List<IMessage> Messages { get; } = [];

        public Task SendAsync(IMessage message, CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    public sealed class ClockFake : Soundtrail.Adapters.Timing.IClockPort
    {
        public DateTimeOffset UtcNow { get; set; } = new(2026, 7, 20, 11, 45, 0, TimeSpan.Zero);
    }
}
