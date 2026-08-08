using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Services.Enrichment.Orchestrator.Features.IncomingWork.OnKnownMusicDataRequested.Composition;
using Soundtrail.Services.Enrichment.Orchestrator.Features.IncomingWork.OnUnknownMusicDataRequested.Composition;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired.Composition;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired.Planning;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupCompleted.Composition;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupWorkReady.Composition;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.RequestedWork;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged.Adapters;
using Soundtrail.Services.Tests.Fakes;
using Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration;

internal sealed class OrchestratorTestAdapters : IFeature
{
    public static OrchestratorTestAdapters Default() => new();

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        OnKnownMusicDataRequestedComposition.Configure(services, new(
            _ => new WorkPlanner(),
            sp => new InMemoryEventStreamRepository<CatalogWorkId>(
                sp.GetRequiredService<DiscoveryEventProjector>().ProjectAsync)));

        OnUnknownMusicDataRequestedComposition.Configure(services, new(
            _ => new WorkPlanner(),
            sp => new SearchForCandidatesFake(
                TestPortResolution.RequireFake<IStoreCatalogSearchCandidatePort, StoreCatalogSearchCandidatePortFake>(sp)),
            sp => sp.GetRequiredService<IEventStreamRepository<CatalogWorkId>>()));

        OnMusicAssessmentRequiredComposition.Configure(services, new(
            _ => new PlanningAssessmentPolicy(Options.Create(new PlanningAssessmentOptions())),
            _ => new DiscoveryPlanningProjectionReaderFake(),
            sp => sp.GetRequiredService<IEventStreamRepository<CatalogWorkId>>()));

        OnLookupWorkReadyComposition.Configure(services, new(
            sp => sp.GetRequiredService<ICommandBus>()));

        OnLookupCompletedComposition.Configure(services, new(
            sp => sp.GetRequiredService<IEventStreamRepository<CatalogWorkId>>(),
            sp => sp.GetRequiredService<ICommandBus>()));
    }
}
