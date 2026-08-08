using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Composition;

public sealed record OnWorkFeedbackChangedPorts(
    Func<IServiceProvider, IStoreDiscoveryFeedbackPort> DiscoveryFeedback);

public static class OnWorkFeedbackChangedComposition
{
    public static void Configure(IServiceCollection services, OnWorkFeedbackChangedPorts ports)
    {
        services.TryAddSingleton(ports.DiscoveryFeedback);
    }
}
