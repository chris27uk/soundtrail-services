using Soundtrail.Adapters.FeatureOrchestration;

namespace Soundtrail.Services.Api.Infrastructure
{
    public interface IApiFeature : IFeature
    {
        void ConfigureApplication(WebApplication app);
    }
}
