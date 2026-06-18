using Microsoft.Extensions.Options;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.SourceBudgets.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.SourceBudgets.Configuration;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.SourceApiBudgetReservation;

internal sealed class SourceApiBudgetReservationTestEnvironment : IDisposable
{
    private readonly IDisposable? cleanup;

    private SourceApiBudgetReservationTestEnvironment(
        IReserveSourceApiBudgetPort port,
        IDisposable? cleanup = null)
    {
        Port = port;
        this.cleanup = cleanup;
    }

    public IReserveSourceApiBudgetPort Port { get; }

    public static SourceApiBudgetReservationTestEnvironment Create(SourceApiBudgetReservationMode mode)
    {
        var options = Options.Create(new SourceApiBudgetsOptions
        {
            MusicBrainz = new SourceApiBudgetPolicyOptions
            {
                MaxRequests = 2,
                WindowSeconds = 60,
                SafetyMarginPercent = 0,
                MinimumSpacingSeconds = 1
            },
            Odesli = new SourceApiBudgetPolicyOptions
            {
                MaxRequests = 2,
                WindowSeconds = 60,
                SafetyMarginPercent = 0,
                MinimumSpacingSeconds = null
            }
        });

        return mode switch
        {
            SourceApiBudgetReservationMode.InProcessFake => new SourceApiBudgetReservationTestEnvironment(
                new InProcessSourceApiBudgetPort(options.Value)),
            SourceApiBudgetReservationMode.RavenCompareExchange => CreateRaven(options),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    private static SourceApiBudgetReservationTestEnvironment CreateRaven(IOptions<SourceApiBudgetsOptions> options)
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        return new SourceApiBudgetReservationTestEnvironment(
            new RavenCompareExchangeSourceApiBudgetPort(raven.Store, options),
            raven);
    }

    public void Dispose() => cleanup?.Dispose();
}
