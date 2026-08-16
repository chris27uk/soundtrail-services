using Microsoft.Extensions.Configuration;
using Soundtrail.Adapters.MusicBrainzDumpFreshness;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupWorkReady.Composition;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration.Orchestrator;

internal sealed class OnLookupWorkReadyTestAdapter(OnLookupWorkReadyPorts ports) : ISociableFeature
{
    public static OnLookupWorkReadyTestAdapter Default() => new(DefaultPorts());

    public static OnLookupWorkReadyTestAdapter With(
        Func<OnLookupWorkReadyPorts, OnLookupWorkReadyPorts> customize) =>
        new(customize(DefaultPorts()));

    public static OnLookupWorkReadyPorts DefaultPorts() =>
        new(
            _ => new MusicBrainzDumpFreshnessEvaluatorFake(),
            sp => new ClockFake(sp.GetRequiredService<SociableScenarioOptions>().UtcNow),
            sp => sp.GetRequiredService<ICommandBus>());

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        OnLookupWorkReadyComposition.Configure(services, ports);
}
