using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.CompositionRoot;

public interface IOrchestratorDependencyProvider
{
    void AddSharedDependencies(IServiceCollection services, IConfiguration configuration);

    void AddJustInTimeSchedulingDependencies(IServiceCollection services, IConfiguration configuration);

    void AddBacklogSchedulingDependencies(IServiceCollection services, IConfiguration configuration);

    void AddEnrichmentResponseDependencies(IServiceCollection services, IConfiguration configuration);
}
