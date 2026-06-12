using Soundtrail.Domain.Responses;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Enrichment.Worker.Features.MusicBrainzLookupExecution;

public interface IGetCanonicalMusicMetadata
{
    Task<SongMetadata?> GetMetadataAsync(
        MusicSearchTerm searchTerm,
        CancellationToken cancellationToken);
}
