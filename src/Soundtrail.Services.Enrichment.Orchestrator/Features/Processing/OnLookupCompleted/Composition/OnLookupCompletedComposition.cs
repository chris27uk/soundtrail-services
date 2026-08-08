using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupCompleted.Composition;

public sealed record OnLookupCompletedPorts(
    Func<IServiceProvider, IEventStreamRepository<CatalogWorkId>> DiscoveryRepository,
    Func<IServiceProvider, ICommandBus> CommandBus);

public static class OnLookupCompletedComposition
{
    public static void Configure(IServiceCollection services, OnLookupCompletedPorts ports)
    {
        services.TryAddSingleton(ports.DiscoveryRepository);
        services.TryAddSingleton(ports.CommandBus);
        services.TryAddScoped<LookupCompletedHandler>();
        services.TryAddScoped<IHandler<CatalogLookupCompleted>, LookupCompletedHandler>();
    }
}
