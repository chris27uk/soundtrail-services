using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup.Lookup;

public interface IGetCanonicalMusicMetadata
{
    Task<SongMetadata?> GetMetadataAsync(
        MusicSearchTerm searchTerm,
        CancellationToken cancellationToken);
}
