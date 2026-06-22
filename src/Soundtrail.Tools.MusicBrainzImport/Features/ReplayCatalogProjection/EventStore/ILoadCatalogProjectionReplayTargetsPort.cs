using Soundtrail.Contracts.Common;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection;

public interface ILoadCatalogProjectionReplayTargetsPort
{
    Task<IReadOnlyList<MusicCatalogId>> LoadAsync(CancellationToken cancellationToken);
}
