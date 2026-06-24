using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata.Pipeline;

public interface ILookupMusicMetadataHandler
{
    Task<MusicCatalogLookupAttempted> Handle(
        LookupMusicMetadataCommand command,
        CancellationToken cancellationToken = default);
}
