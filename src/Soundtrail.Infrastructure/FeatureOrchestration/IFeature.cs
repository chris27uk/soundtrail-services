using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Soundtrail.Adapters.FeatureOrchestration
{
    public interface IFeature
    {
        void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    }
}
