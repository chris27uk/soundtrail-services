using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged.Adapters;

public interface IStoreCatalogSearchCandidatePort
{
    Task StoreAsync(CatalogSearchCandidateProjection projection, CancellationToken cancellationToken);
}
