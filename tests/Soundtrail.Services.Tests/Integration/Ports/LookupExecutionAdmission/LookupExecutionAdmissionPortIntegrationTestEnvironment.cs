using Microsoft.Extensions.Options;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.ExecutionAdmission;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;
using StackExchange.Redis;

namespace Soundtrail.Services.Tests.Integration.Ports.LookupExecutionAdmission;

internal sealed class LookupExecutionAdmissionPortIntegrationTestEnvironment : IAsyncDisposable
{
    private readonly LocalRedisTestServer redisServer;
    private readonly IConnectionMultiplexer connectionMultiplexer;

    private LookupExecutionAdmissionPortIntegrationTestEnvironment(
        LocalRedisTestServer redisServer,
        IConnectionMultiplexer connectionMultiplexer,
        RedisLookupExecutionAdmissionPort subject,
        DateTimeOffset requestedAt)
    {
        this.redisServer = redisServer;
        this.connectionMultiplexer = connectionMultiplexer;
        Subject = subject;
        RequestedAt = requestedAt;
    }

    public RedisLookupExecutionAdmissionPort Subject { get; }

    public DateTimeOffset RequestedAt { get; }

    public static async Task<LookupExecutionAdmissionPortIntegrationTestEnvironment> CreateAsync()
    {
        return await CreateAsync(maxRequests: 1, activeLeaseSeconds: 300);
    }

    public static async Task<LookupExecutionAdmissionPortIntegrationTestEnvironment> CreateAsync(
        int maxRequests,
        int activeLeaseSeconds)
    {
        var redisServer = await LocalRedisTestServer.StartAsync();
        var connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(redisServer.ConnectionString);
        var subject = new RedisLookupExecutionAdmissionPort(
            connectionMultiplexer,
            Options.Create(new SourceApiBudgetsOptions
            {
                Kworb = new ApiBudgetPolicy
                {
                    MaxRequests = maxRequests,
                    MinimumSpacingSeconds = 1,
                    SafetyMarginPercent = 0,
                    WindowSeconds = 60
                }
            }),
            Options.Create(new RedisLookupExecutionAdmissionOptions
            {
                ActiveLeaseSeconds = activeLeaseSeconds,
                KeyPrefix = $"lookup-execution-admission-port-tests:{Guid.NewGuid():N}"
            }));

        return new LookupExecutionAdmissionPortIntegrationTestEnvironment(
            redisServer,
            connectionMultiplexer,
            subject,
            new DateTimeOffset(2026, 7, 21, 9, 0, 0, TimeSpan.Zero));
    }

    public LookupExecutionAdmissionRequest CreateRequest(string messageId) =>
        new(LookupSource.Kworb, MessageId.For(messageId), RequestedAt);

    public ValueTask DisposeAsync()
    {
        connectionMultiplexer.Dispose();
        return redisServer.DisposeAsync();
    }
}
