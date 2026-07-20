using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Aggregates;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Features.LookupPlaylistTracks;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.ExecutionAdmission;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;
using Soundtrail.Services.Tests.Integration.Ports;
using StackExchange.Redis;

namespace Soundtrail.Services.Tests.Integration.Worker.LookupPlaylistTracks;

internal sealed class LookupPlaylistTracksDecoratorIntegrationTestEnvironment : IAsyncDisposable
{
    private readonly LocalRedisTestServer? redisServer;
    private readonly IConnectionMultiplexer? connectionMultiplexer;
    private readonly IDocumentStore? documentStore;
    private readonly List<string> cleanupDocumentIds = [];
    private readonly IAsyncDocumentSession? receiptSession;

    private LookupPlaylistTracksDecoratorIntegrationTestEnvironment(
        CommandBusFake commandBus,
        InnerHandlerFake innerHandler,
        ClockFake clock,
        LocalRedisTestServer? redisServer = null,
        IConnectionMultiplexer? connectionMultiplexer = null,
        RedisLookupExecutionAdmissionPort? admissionPort = null,
        IDocumentStore? documentStore = null,
        IAsyncDocumentSession? receiptSession = null)
    {
        CommandBus = commandBus;
        InnerHandler = innerHandler;
        Clock = clock;
        this.redisServer = redisServer;
        this.connectionMultiplexer = connectionMultiplexer;
        AdmissionPort = admissionPort;
        this.documentStore = documentStore;
        this.receiptSession = receiptSession;
    }

    public CommandBusFake CommandBus { get; }

    public InnerHandlerFake InnerHandler { get; }

    public ClockFake Clock { get; }

    public RedisLookupExecutionAdmissionPort? AdmissionPort { get; }

    public static async Task<LookupPlaylistTracksDecoratorIntegrationTestEnvironment> CreateForAdmissionAsync()
    {
        var redisServer = await LocalRedisTestServer.StartAsync();
        var multiplexer = await ConnectionMultiplexer.ConnectAsync(redisServer.ConnectionString);
        var port = new RedisLookupExecutionAdmissionPort(
            multiplexer,
            Options.Create(new SourceApiBudgetsOptions
            {
                Kworb = new ApiBudgetPolicy
                {
                    MaxRequests = 1,
                    MinimumSpacingSeconds = 1,
                    SafetyMarginPercent = 0,
                    WindowSeconds = 60
                }
            }),
            Options.Create(new RedisLookupExecutionAdmissionOptions
            {
                ActiveLeaseSeconds = 300,
                KeyPrefix = $"lookup-execution-admission-tests:{Guid.NewGuid():N}"
            }));

        return new LookupPlaylistTracksDecoratorIntegrationTestEnvironment(
            new CommandBusFake(),
            new InnerHandlerFake(),
            new ClockFake(),
            redisServer,
            multiplexer,
            port);
    }

    public static Task<LookupPlaylistTracksDecoratorIntegrationTestEnvironment> CreateForIdempotencyAsync()
    {
        var store = EmbeddedRavenTestServer.CreateDocumentStore();
        var session = store.OpenAsyncSession();
        return Task.FromResult(
            new LookupPlaylistTracksDecoratorIntegrationTestEnvironment(
                new CommandBusFake(),
                new InnerHandlerFake(),
                new ClockFake(),
                documentStore: store,
                receiptSession: session));
    }

    public LookupPlaylistTracksByProviderMessage CreateRequest(
        string? messageId = null,
        DateTimeOffset? createdAt = null) =>
        CreateTrackedRequest(messageId, createdAt);

    public AdmittedLookupPlaylistTracksByProviderHandlerDecorator CreateAdmissionSubject() =>
        new(InnerHandler, CommandBus, AdmissionPort!, Clock);

    public async Task<IdempotentLookupPlaylistTracksByProviderHandlerDecorator> CreateIdempotencySubjectAsync()
    {
        var store = new RavenLookupExecutionReceiptStore(receiptSession!);
        return new IdempotentLookupPlaylistTracksByProviderHandlerDecorator(InnerHandler, store, CommandBus, Clock);
    }

    public Task SaveReceiptChangesAsync()
    {
        return receiptSession?.SaveChangesAsync() ?? Task.CompletedTask;
    }

    public async Task<RavenLookupExecutionReceiptDto?> LoadReceiptAsync(MessageId messageId)
    {
        using var session = documentStore!.OpenAsyncSession();
        return await session.LoadAsync<RavenLookupExecutionReceiptDto>(
            RavenLookupExecutionReceiptDto.GetDocumentId(messageId.Value));
    }

    public async ValueTask DisposeAsync()
    {
        if (receiptSession is not null)
        {
            receiptSession.Dispose();
        }

        if (connectionMultiplexer is not null)
        {
            await connectionMultiplexer.CloseAsync();
            connectionMultiplexer.Dispose();
        }

        if (redisServer is not null)
        {
            await redisServer.DisposeAsync();
        }

        if (documentStore is not null)
        {
            foreach (var cleanupDocumentId in cleanupDocumentIds)
            {
                await EmbeddedRavenTestServer.DisposeAsync(documentStore, cleanupDocumentId);
            }
        }
    }

    private LookupPlaylistTracksByProviderMessage CreateTrackedRequest(
        string? messageId,
        DateTimeOffset? createdAt)
    {
        var resolvedMessageId = messageId ?? $"msg-playlist-lookup-{Guid.NewGuid():N}";
        var request = new LookupPlaylistTracksByProviderMessage(
            MessageId.For(resolvedMessageId),
            CorrelationId.From($"corr:{resolvedMessageId}"),
            createdAt ?? new DateTimeOffset(2026, 7, 20, 10, 30, 0, TimeSpan.Zero),
            LookupPriorityBand.High,
            PlaylistId.FromPlaylistName("WorldwideSongChart"),
            ProviderName.Spotify);

        if (documentStore is not null)
        {
            cleanupDocumentIds.Add(RavenLookupExecutionReceiptDto.GetDocumentId(request.Id.Value));
        }

        return request;
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

    public sealed class InnerHandlerFake : IHandler<LookupPlaylistTracksByProviderMessage>
    {
        public int Calls { get; private set; }

        public Exception? ExceptionToThrow { get; set; }

        public Task Handle(LookupPlaylistTracksByProviderMessage request, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.CompletedTask;
        }
    }

    public sealed class ClockFake : IClockPort
    {
        public DateTimeOffset UtcNow { get; set; } = new(2026, 7, 20, 11, 45, 0, TimeSpan.Zero);
    }
}
