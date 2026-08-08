using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Assesment;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired.Planning;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired.Composition;

public sealed record OnMusicAssessmentRequiredPorts(
    Func<IServiceProvider, IPlanningAssessmentPolicy> AssessmentPolicy,
    Func<IServiceProvider, IDiscoveryPlanningProjectionReader> PlanningProjection,
    Func<IServiceProvider, IEventStreamRepository<CatalogWorkId>> DiscoveryRepository);

public static class OnMusicAssessmentRequiredComposition
{
    public static void Configure(IServiceCollection services, OnMusicAssessmentRequiredPorts ports)
    {
        services.TryAddSingleton(ports.AssessmentPolicy);
        services.TryAddSingleton(ports.PlanningProjection);
        services.TryAddSingleton(ports.DiscoveryRepository);
        services.TryAddScoped<OnMusicAssessmentRequiredHandler>();
        services.TryAddScoped<IHandler<AssessWorkMessage>, OnMusicAssessmentRequiredHandler>();
    }
}
