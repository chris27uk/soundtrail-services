using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.CompositionRoot;

public interface IOrchestratorDependencyProvider
{
    void AddSharedDependencies(IServiceCollection services, IConfiguration configuration);

    void AddOnCatalogSearchRequestedDependencies(IServiceCollection services, IConfiguration configuration);

    void AddOnNextMusicTracksRequestedForLookupDependencies(IServiceCollection services, IConfiguration configuration);
}
