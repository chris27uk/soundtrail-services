using Soundtrail.Domain.Catalog;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.EventStore;

public interface ILoadCatalogProjectionReplayTargetsPort
{
    Task<IReadOnlyList<ArtistId>> LoadAsync(CancellationToken cancellationToken);
}
