using Microsoft.Extensions.Configuration;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Services.Enrichment.Orchestrator.Features.IncomingWork.OnUnknownMusicDataRequested.Composition;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.RequestedWork;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged.Adapters;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;
using Soundtrail.Services.Tests.Unit.Sociable.Features.Search;
using Soundtrail.Services.Tests.Unit.Sociable.Features.Search.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.Search.Support.DependencyInjection;

internal sealed class OnUnknownMusicDataRequestedTestAdapter(OnUnknownMusicDataRequestedPorts ports) : ISociableFeature
{
    public static OnUnknownMusicDataRequestedTestAdapter Default() => new(DefaultPorts());

    public static OnUnknownMusicDataRequestedTestAdapter With(
        Func<OnUnknownMusicDataRequestedPorts, OnUnknownMusicDataRequestedPorts> customize) =>
        new(customize(DefaultPorts()));

    public static OnUnknownMusicDataRequestedPorts DefaultPorts() =>
        new(
            _ => new WorkPlanner(),
            sp => new SearchForCandidatesFake(
                TestPortResolution.RequireFake<IStoreCatalogSearchCandidatePort, StoreCatalogSearchCandidatePortFake>(sp)),
            sp => sp.GetRequiredService<IEventStreamRepository<CatalogWorkId>>());

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        OnUnknownMusicDataRequestedComposition.Configure(services, ports);
}
