using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Search.Contract;

namespace Soundtrail.Services.Api.Features.Search.Adapters;

public interface IDiscoveryFeedbackPort
{
    Task<DiscoveryFeedbackResponse?> GetAsync(EnrichmentTarget target, CancellationToken cancellationToken);
}
