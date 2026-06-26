using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Ports;

public interface ISavePotentialCatalogLookupWorkPort
{
    Task SaveAsync(PotentialCatalogLookupWorkState work, CancellationToken cancellationToken);
}
