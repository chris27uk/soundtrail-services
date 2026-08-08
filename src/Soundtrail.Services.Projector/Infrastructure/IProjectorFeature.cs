using Microsoft.AspNetCore.Builder;
using Soundtrail.Adapters.FeatureOrchestration;

namespace Soundtrail.Services.Internal.Projector.Infrastructure;

public interface IProjectorFeature : IFeature
{
    void ConfigureApplication(WebApplication app);
}
