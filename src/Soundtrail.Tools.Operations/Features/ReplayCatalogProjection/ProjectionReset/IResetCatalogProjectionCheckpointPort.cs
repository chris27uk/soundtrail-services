using Soundtrail.Domain.Catalog;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.ProjectionReset;

public interface IResetCatalogProjectionCheckpointPort
{
    Task ResetAsync(
        ArtistId artistId,
        CancellationToken cancellationToken);
}
