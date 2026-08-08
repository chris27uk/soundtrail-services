using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.RequestedWork;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.IncomingWork.OnKnownMusicDataRequested.Composition;

public sealed class OnKnownMusicDataRequestedPorts(
    Func<IServiceProvider, IWorkPlanner> workPlanner,
    Func<IServiceProvider, IEventStreamRepository<CatalogWorkId>> discoveryRepository)
{
    public Func<IServiceProvider, IWorkPlanner> WorkPlanner { get; } = workPlanner;

    public Func<IServiceProvider, IEventStreamRepository<CatalogWorkId>> DiscoveryRepository { get; } =
        discoveryRepository;
}

public static class OnKnownMusicDataRequestedComposition
{
    public static void Configure(IServiceCollection services, OnKnownMusicDataRequestedPorts ports)
    {
        services.TryAddSingleton(ports.WorkPlanner);
        services.TryAddSingleton(ports.DiscoveryRepository);
        services.TryAddScoped<IHandler<RequestKnownMusicDataMessage>, OnKnownMusicDataRequestedHandler>();
    }
}
