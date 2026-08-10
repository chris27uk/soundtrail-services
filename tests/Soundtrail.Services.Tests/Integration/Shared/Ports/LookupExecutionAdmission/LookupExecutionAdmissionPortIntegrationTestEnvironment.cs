using Microsoft.Extensions.Options;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.ExecutionAdmission;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;
using Soundtrail.Services.Tests.Integration.Shared.Infrastructure;
using StackExchange.Redis;

namespace Soundtrail.Services.Tests.Integration.Shared.Ports.LookupExecutionAdmission;

internal sealed class LookupExecutionAdmissionPortIntegrationTestEnvironment : IAsyncDisposable
{
    private LookupExecutionAdmissionPortIntegrationTestEnvironment(
        RedisLookupExecutionAdmissionPort subject,
        DateTimeOffset requestedAt)
    {
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
        int activeLeaseSeconds,
        int minimumSpacingSeconds = 1)
    {
        var connectionMultiplexer = await LocalRedisTestServer.GetSharedMultiplexerAsync();
        var subject = new RedisLookupExecutionAdmissionPort(
            connectionMultiplexer,
            Options.Create(new SourceApiBudgetsOptions
            {
                Kworb = new ApiBudgetPolicy
                {
                    MaxRequests = maxRequests,
                    MinimumSpacingSeconds = minimumSpacingSeconds,
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
            subject,
            new DateTimeOffset(2026, 7, 21, 9, 0, 0, TimeSpan.Zero));
    }

    public LookupExecutionAdmissionRequest CreateRequest(string messageId) =>
        new(LookupSource.Kworb, MessageId.For(messageId), RequestedAt);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
