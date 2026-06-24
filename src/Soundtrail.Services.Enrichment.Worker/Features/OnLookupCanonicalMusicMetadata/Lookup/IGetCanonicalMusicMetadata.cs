using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupCanonicalMusicMetadata.Lookup;

public interface IGetCanonicalMusicMetadata
{
    Task<SongMetadata?> GetMetadataAsync(
        MusicSearchTerm searchTerm,
        CancellationToken cancellationToken);
}
