using Soundtrail.Domain.Responses;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Enrichment.Worker.Features.MusicBrainzLookupExecution;

public interface IMusicBrainzMetadataSource
{
    Task<SongMetadata?> GetMetadataAsync(
        CanonicalMusicMetadataLookup lookup,
        CancellationToken cancellationToken);
}
