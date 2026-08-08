using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Composition;

public sealed class OnWorkFeedbackChangedPorts(
    Func<IServiceProvider, IStoreDiscoveryFeedbackPort> discoveryFeedback)
{
    public Func<IServiceProvider, IStoreDiscoveryFeedbackPort> DiscoveryFeedback { get; } = discoveryFeedback;
}

public static class OnWorkFeedbackChangedComposition
{
    public static void Configure(IServiceCollection services, OnWorkFeedbackChangedPorts ports)
    {
        services.TryAddSingleton(ports.DiscoveryFeedback);
    }
}
