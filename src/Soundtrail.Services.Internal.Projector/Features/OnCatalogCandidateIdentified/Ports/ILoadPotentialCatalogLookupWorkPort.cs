using Soundtrail.Contracts.Common;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Ports;

public interface ILoadPotentialCatalogLookupWorkPort
{
    Task<PotentialCatalogLookupWorkState> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken);
}
