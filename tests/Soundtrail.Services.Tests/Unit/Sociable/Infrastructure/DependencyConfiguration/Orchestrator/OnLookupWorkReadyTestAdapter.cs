using Microsoft.Extensions.Configuration;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupWorkReady.Composition;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration.Orchestrator;

internal sealed class OnLookupWorkReadyTestAdapter(OnLookupWorkReadyPorts ports) : ISociableFeature
{
    public static OnLookupWorkReadyTestAdapter Default() => new(DefaultPorts());

    public static OnLookupWorkReadyTestAdapter With(
        Func<OnLookupWorkReadyPorts, OnLookupWorkReadyPorts> customize) =>
        new(customize(DefaultPorts()));

    public static OnLookupWorkReadyPorts DefaultPorts() =>
        new(sp => sp.GetRequiredService<ICommandBus>());

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        OnLookupWorkReadyComposition.Configure(services, ports);
}
