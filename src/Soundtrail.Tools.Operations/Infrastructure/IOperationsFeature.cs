using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Soundtrail.Adapters.FeatureOrchestration;
using Wolverine;

namespace Soundtrail.Tools.Operations.Infrastructure;

public interface IOperationsFeature : IFeature
{
    void ConfigureMessaging(WolverineOptions options, IConfiguration configuration, IHostEnvironment environment);
}
