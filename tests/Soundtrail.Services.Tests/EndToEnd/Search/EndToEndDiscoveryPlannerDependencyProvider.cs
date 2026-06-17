using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.CompositionRoot;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Raven;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.EndToEnd.Search;

internal sealed class EndToEndDiscoveryPlannerDependencyProvider(
    FakeMusicCatalogCandidateSearch candidateSearch) : IDiscoveryPlannerDependencyProvider
{
    public void AddSharedDependencies(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSchedulerRavenDocumentStore(configuration);
    }

    public void AddJustInTimeSchedulingDependencies(IServiceCollection services, IConfiguration configuration)
    {
        services.Replace(ServiceDescriptor.Singleton<IMusicCatalogCandidateSearch>(candidateSearch));
    }

    public void AddBacklogSchedulingDependencies(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void AddEnrichmentResponseDependencies(IServiceCollection services, IConfiguration configuration)
    {
    }
}
