using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Enrichment.Responses;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata.Pipeline;

public interface ILookupTrackMetadataHandler
{
    Task<MusicCatalogLookupAttempted> Handle(
        LookupTrackMetadataCommand command,
        CancellationToken cancellationToken = default);
}
