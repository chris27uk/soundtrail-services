using Microsoft.Extensions.Configuration;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupCompleted.Composition;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration.Orchestrator;

internal sealed class OnLookupCompletedTestAdapter(OnLookupCompletedPorts ports) : ISociableFeature
{
    public static OnLookupCompletedTestAdapter Default() => new(DefaultPorts());

    public static OnLookupCompletedTestAdapter With(
        Func<OnLookupCompletedPorts, OnLookupCompletedPorts> customize) =>
        new(customize(DefaultPorts()));

    public static OnLookupCompletedPorts DefaultPorts() =>
        new(
            sp => sp.GetRequiredService<IEventStreamRepository<CatalogWorkId>>(),
            sp => sp.GetRequiredService<ICommandBus>());

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        OnLookupCompletedComposition.Configure(services, ports);
}
