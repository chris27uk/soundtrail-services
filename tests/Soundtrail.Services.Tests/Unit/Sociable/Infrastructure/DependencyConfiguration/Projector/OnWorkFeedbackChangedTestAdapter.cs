using Microsoft.Extensions.Configuration;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Composition;
using Soundtrail.Services.Internal.Projector.Infrastructure;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration.Projector;

internal sealed class OnWorkFeedbackChangedTestAdapter(OnWorkFeedbackChangedPorts ports) : ISociableFeature, IProjectorFeature
{
    public static OnWorkFeedbackChangedTestAdapter Default() => new(DefaultPorts());

    public static OnWorkFeedbackChangedTestAdapter With(
        Func<OnWorkFeedbackChangedPorts, OnWorkFeedbackChangedPorts> customize) =>
        new(customize(DefaultPorts()));

    public static OnWorkFeedbackChangedPorts DefaultPorts() =>
        new(_ => new StoreDiscoveryFeedbackPortFake());

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        OnWorkFeedbackChangedComposition.Configure(services, ports);

    public void ConfigureApplication(WebApplication app)
    {
    }
}
