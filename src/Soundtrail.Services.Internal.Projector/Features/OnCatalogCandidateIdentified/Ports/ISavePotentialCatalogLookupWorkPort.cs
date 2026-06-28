using Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Ports;

public interface ISavePotentialCatalogLookupWorkPort
{
    Task SaveAsync(PotentialCatalogLookupWorkState work, CancellationToken cancellationToken);
}
