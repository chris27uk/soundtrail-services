using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.CompositionRoot;

public interface IWorkerDependencyProvider
{
    void AddSharedDependencies(IServiceCollection services, IConfiguration configuration);

    void AddLookupMusicMetadataDependencies(IServiceCollection services, IConfiguration configuration);

    void AddLookupStreamingLocationsDependencies(IServiceCollection services, IConfiguration configuration);
}
