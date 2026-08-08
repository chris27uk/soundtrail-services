using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;

namespace Soundtrail.Services.Tests.Fakes;

internal sealed class DiscoveryFeedbackPortFake : IDiscoveryFeedbackPort
{
    private readonly Func<EnrichmentTarget, CancellationToken, Task<DiscoveryFeedbackResponse?>>? resolver;
    private DiscoveryFeedbackResponse? response;

    public DiscoveryFeedbackPortFake()
    {
    }

    private DiscoveryFeedbackPortFake(
        Func<EnrichmentTarget, CancellationToken, Task<DiscoveryFeedbackResponse?>> resolver) =>
        this.resolver = resolver;

    public EnrichmentTarget? RequestedTarget { get; private set; }

    public DiscoveryFeedbackResponse? Response => response;

    public static DiscoveryFeedbackPortFake Create(
        Func<EnrichmentTarget, CancellationToken, Task<DiscoveryFeedbackResponse?>> resolver) =>
        new(resolver);

    public void Seed(DiscoveryFeedbackResponse? feedback) => response = feedback;

    public Task<DiscoveryFeedbackResponse?> GetAsync(EnrichmentTarget target, CancellationToken cancellationToken)
    {
        RequestedTarget = target;
        return resolver is null
            ? Task.FromResult(response)
            : resolver(target, cancellationToken);
    }
}
