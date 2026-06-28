using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateRecorded.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateRecorded.Ports;

public interface ISavePotentialCatalogLookupWorkPort
{
    Task SaveAsync(PotentialCatalogLookupWorkState work, CancellationToken cancellationToken);
}
