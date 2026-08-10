using Microsoft.Extensions.Options;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.ExecutionAdmission;
using Soundtrail.Services.Tests.Integration.Shared.Infrastructure;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Integration.Shared.Worker;

internal sealed class LookupExecutionAdmissionDecoratorIntegrationTestEnvironment : IAsyncDisposable
{
    private LookupExecutionAdmissionDecoratorIntegrationTestEnvironment(
        RedisLookupExecutionAdmissionPort admissionPort)
    {
        AdmissionPort = admissionPort;
        CommandBus = new CommandBusFake();
        Clock = new ClockFake(new DateTimeOffset(2026, 7, 21, 10, 0, 0, TimeSpan.Zero));
    }

    public RedisLookupExecutionAdmissionPort AdmissionPort { get; }

    public CommandBusFake CommandBus { get; }

    public ClockFake Clock { get; }

    public static async Task<LookupExecutionAdmissionDecoratorIntegrationTestEnvironment> CreateAsync(
        int maxRequests = 2,
        int activeLeaseSeconds = 300)
    {
        var connectionMultiplexer = await LocalRedisTestServer.GetSharedMultiplexerAsync();
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

        return new LookupExecutionAdmissionDecoratorIntegrationTestEnvironment(admissionPort);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static ApiBudgetPolicy CreatePolicy(int maxRequests) =>
        new()
        {
            MaxRequests = maxRequests,
            MinimumSpacingSeconds = 1,
            SafetyMarginPercent = 0,
            WindowSeconds = 60
        };
}
