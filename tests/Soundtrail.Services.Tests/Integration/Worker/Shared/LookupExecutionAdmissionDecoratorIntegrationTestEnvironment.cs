using Microsoft.Extensions.Options;
using Soundtrail.Adapters.Timing;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.ExecutionAdmission;
using Soundtrail.Services.Tests.Integration.Ports;
using StackExchange.Redis;

namespace Soundtrail.Services.Tests.Integration.Worker.Shared;

internal sealed class LookupExecutionAdmissionDecoratorIntegrationTestEnvironment : IAsyncDisposable
{
    private readonly LocalRedisTestServer redisServer;
    private readonly IConnectionMultiplexer connectionMultiplexer;

    private LookupExecutionAdmissionDecoratorIntegrationTestEnvironment(
        LocalRedisTestServer redisServer,
        IConnectionMultiplexer connectionMultiplexer,
        RedisLookupExecutionAdmissionPort admissionPort)
    {
        this.redisServer = redisServer;
        this.connectionMultiplexer = connectionMultiplexer;
        AdmissionPort = admissionPort;
        CommandBus = new CommandBusFake();
        Clock = new ClockFake();
    }

    public RedisLookupExecutionAdmissionPort AdmissionPort { get; }

    public CommandBusFake CommandBus { get; }

    public ClockFake Clock { get; }

    public static async Task<LookupExecutionAdmissionDecoratorIntegrationTestEnvironment> CreateAsync(
        int maxRequests = 2,
        int activeLeaseSeconds = 300)
    {
        var redisServer = await LocalRedisTestServer.StartAsync();
        var connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(redisServer.ConnectionString);
        var budgets = new SourceApiBudgetsOptions
        {
            MusicBrainz = CreatePolicy(maxRequests),
            Odesli = CreatePolicy(maxRequests),
            Kworb = CreatePolicy(maxRequests)
        };

        var admissionPort = new RedisLookupExecutionAdmissionPort(
            connectionMultiplexer,
            Options.Create(budgets),
            Options.Create(new RedisLookupExecutionAdmissionOptions
            {
                ActiveLeaseSeconds = activeLeaseSeconds,
                KeyPrefix = $"lookup-execution-decorator-tests:{Guid.NewGuid():N}"
            }));

        return new LookupExecutionAdmissionDecoratorIntegrationTestEnvironment(
            redisServer,
            connectionMultiplexer,
            admissionPort);
    }

    public ValueTask DisposeAsync()
    {
        connectionMultiplexer.Dispose();
        return redisServer.DisposeAsync();
    }

    private static ApiBudgetPolicy CreatePolicy(int maxRequests) =>
        new()
        {
            MaxRequests = maxRequests,
            MinimumSpacingSeconds = 1,
            SafetyMarginPercent = 0,
            WindowSeconds = 60
        };

    public sealed class ClockFake : IClockPort
    {
        public DateTimeOffset UtcNow { get; set; } = new(2026, 7, 21, 10, 0, 0, TimeSpan.Zero);
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
}
