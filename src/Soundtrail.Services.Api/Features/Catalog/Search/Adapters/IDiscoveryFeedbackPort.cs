using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.Search.Adapters;

public interface IDiscoveryFeedbackPort
{
    Task<DiscoveryFeedbackResponse?> GetAsync(EnrichmentTarget target, CancellationToken cancellationToken);
}
