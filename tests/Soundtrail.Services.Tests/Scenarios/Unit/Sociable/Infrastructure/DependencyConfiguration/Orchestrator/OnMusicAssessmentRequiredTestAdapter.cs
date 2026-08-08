using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired.Composition;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired.Planning;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration.Orchestrator;

internal sealed class OnMusicAssessmentRequiredTestAdapter(OnMusicAssessmentRequiredPorts ports) : ISociableFeature
{
    public static OnMusicAssessmentRequiredTestAdapter Default() => new(DefaultPorts());

    public static OnMusicAssessmentRequiredTestAdapter With(
        Func<OnMusicAssessmentRequiredPorts, OnMusicAssessmentRequiredPorts> customize) =>
        new(customize(DefaultPorts()));

    public static OnMusicAssessmentRequiredPorts DefaultPorts() =>
        new(
            _ => new PlanningAssessmentPolicy(Options.Create(new PlanningAssessmentOptions())),
            _ => new DiscoveryPlanningProjectionReaderFake(),
            sp => sp.GetRequiredService<IEventStreamRepository<CatalogWorkId>>());

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        OnMusicAssessmentRequiredComposition.Configure(services, ports);
}
