using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata.Lookup;

public interface IGetMusicMetadata
{
    Task<SongMetadata?> GetMetadataAsync(
        MusicSearchTerm searchTerm,
        CancellationToken cancellationToken);
}
