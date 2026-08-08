using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Candidates;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.RequestedWork;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.IncomingWork.OnUnknownMusicDataRequested.Composition;

public sealed record OnUnknownMusicDataRequestedPorts(
    Func<IServiceProvider, IWorkPlanner> WorkPlanner,
    Func<IServiceProvider, ISearchForCandidates> SearchForCandidates,
    Func<IServiceProvider, IEventStreamRepository<CatalogWorkId>> DiscoveryRepository);

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
