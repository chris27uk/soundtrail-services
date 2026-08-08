using Microsoft.Extensions.Configuration;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Services.Enrichment.Orchestrator.Features.IncomingWork.OnKnownMusicDataRequested.Composition;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.RequestedWork;
using Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration.Orchestrator;

internal sealed class OnKnownMusicDataRequestedTestAdapter(OnKnownMusicDataRequestedPorts ports) : ISociableFeature
{
    public static OnKnownMusicDataRequestedTestAdapter Default() => new(DefaultPorts());

    public static OnKnownMusicDataRequestedTestAdapter With(
        Func<OnKnownMusicDataRequestedPorts, OnKnownMusicDataRequestedPorts> customize) =>
        new(customize(DefaultPorts()));

    public static OnKnownMusicDataRequestedPorts DefaultPorts() =>
        new(
            _ => new WorkPlanner(),
            sp => new InMemoryEventStreamRepository<CatalogWorkId>(
                sp.GetRequiredService<DiscoveryEventProjector>().ProjectAsync));

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        OnKnownMusicDataRequestedComposition.Configure(services, ports);
}
