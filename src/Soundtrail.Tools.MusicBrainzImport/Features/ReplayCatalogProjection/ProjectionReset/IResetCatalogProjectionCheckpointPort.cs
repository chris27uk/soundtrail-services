using Soundtrail.Contracts.Common;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection;

public interface IResetCatalogProjectionCheckpointPort
{
    Task ResetAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken);
}
