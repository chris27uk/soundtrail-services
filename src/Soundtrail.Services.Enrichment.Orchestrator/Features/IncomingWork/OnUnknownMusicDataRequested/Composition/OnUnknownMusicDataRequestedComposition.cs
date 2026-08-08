using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Candidates;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.RequestedWork;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.IncomingWork.OnUnknownMusicDataRequested.Composition;

public sealed class OnUnknownMusicDataRequestedPorts(
    Func<IServiceProvider, IWorkPlanner> workPlanner,
    Func<IServiceProvider, ISearchForCandidates> searchForCandidates,
    Func<IServiceProvider, IEventStreamRepository<CatalogWorkId>> discoveryRepository)
{
    public Func<IServiceProvider, IWorkPlanner> WorkPlanner { get; } = workPlanner;

    public Func<IServiceProvider, ISearchForCandidates> SearchForCandidates { get; } = searchForCandidates;

    public Func<IServiceProvider, IEventStreamRepository<CatalogWorkId>> DiscoveryRepository { get; } =
        discoveryRepository;
}

public static class OnUnknownMusicDataRequestedComposition
{
    public static void Configure(IServiceCollection services, OnUnknownMusicDataRequestedPorts ports)
    {
        services.TryAddSingleton(ports.WorkPlanner);
        services.TryAddSingleton(ports.SearchForCandidates);
        services.TryAddSingleton(ports.DiscoveryRepository);
        services.TryAddScoped<IHandler<RequestUnknownMusicDataMessage>, OnUnknownMusicDataRequestedHandler>();
    }
}
