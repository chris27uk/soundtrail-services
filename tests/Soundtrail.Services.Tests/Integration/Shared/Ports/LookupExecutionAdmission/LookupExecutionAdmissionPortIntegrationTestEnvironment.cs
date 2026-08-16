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

    public static Task<LookupExecutionAdmissionPortIntegrationTestEnvironment> CreateAsync(
        int maxRequests,
        int activeLeaseSeconds,
        int minimumSpacingSeconds = 1,
        int safetyMarginPercent = 0) =>
        CreateAsync(
            LookupSource.Kworb,
            maxRequests,
            activeLeaseSeconds,
            minimumSpacingSeconds,
            safetyMarginPercent);

    public static async Task<LookupExecutionAdmissionPortIntegrationTestEnvironment> CreateForMusicBrainzAsync(
        int maxRequests,
        int activeLeaseSeconds = 300,
        int minimumSpacingSeconds = 0,
        int safetyMarginPercent = 0) =>
        await CreateAsync(
            LookupSource.MusicBrainz,
            maxRequests,
            activeLeaseSeconds,
            minimumSpacingSeconds,
            safetyMarginPercent);

    private static async Task<LookupExecutionAdmissionPortIntegrationTestEnvironment> CreateAsync(
        LookupSource provider,
        int maxRequests,
        int activeLeaseSeconds,
        int minimumSpacingSeconds,
        int safetyMarginPercent)
    {
        var connectionMultiplexer = await LocalRedisTestServer.GetSharedMultiplexerAsync();
        var policy = new ApiBudgetPolicy
        {
            MaxRequests = maxRequests,
            MinimumSpacingSeconds = minimumSpacingSeconds,
            SafetyMarginPercent = safetyMarginPercent,
            WindowSeconds = 60
        };
        var budgets = new SourceApiBudgetsOptions();
        if (provider == LookupSource.MusicBrainz)
        {
            budgets.MusicBrainz = policy;
        }
        else if (provider == LookupSource.Kworb)
        {
            budgets.Kworb = policy;
        }
        else if (provider == LookupSource.Odesli)
        {
            budgets.Odesli = policy;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
        }

        var subject = new RedisLookupExecutionAdmissionPort(
            connectionMultiplexer,
            Options.Create(budgets),
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
        CreateRequest(LookupSource.Kworb, messageId);

    public LookupExecutionAdmissionRequest CreateMusicBrainzRequest(string messageId) =>
        CreateRequest(LookupSource.MusicBrainz, messageId);

    public LookupExecutionAdmissionRequest CreateRequest(LookupSource provider, string messageId) =>
        new(provider, MessageId.For(messageId), RequestedAt);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
