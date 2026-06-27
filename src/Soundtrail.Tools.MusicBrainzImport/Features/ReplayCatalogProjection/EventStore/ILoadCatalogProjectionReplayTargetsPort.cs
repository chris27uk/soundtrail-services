using Soundtrail.Contracts.Common;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.EventStore;

public interface ILoadCatalogProjectionReplayTargetsPort
{
    Task<IReadOnlyList<MusicCatalogId>> LoadAsync(CancellationToken cancellationToken);
}
