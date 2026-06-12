using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.DiscoveryPlanner;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.LocalSearch;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.CompositionRoot;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.EndToEnd.Search;

internal sealed class EndToEndDiscoveryPlannerDependencyProvider(
    FakeMusicCatalogCandidateSearch candidateSearch,
    LocalMusicTrackSearchFake localSearch) : IDiscoveryPlannerDependencyProvider
{
    public void AddSharedDependencies(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void AddJustInTimeSchedulingDependencies(IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<IMusicCatalogCandidateSearch>(candidateSearch);
        services.TryAddSingleton<ILocalMusicTrackSearch>(localSearch);
    }

    public void AddBacklogSchedulingDependencies(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void AddEnrichmentResponseDependencies(IServiceCollection services, IConfiguration configuration)
    {
    }
}
