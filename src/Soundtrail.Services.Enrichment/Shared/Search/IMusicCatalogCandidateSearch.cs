using Soundtrail.Services.Features.Search.TrackSearch;

namespace Soundtrail.Services.Enrichment.Shared.Search;

public interface IMusicCatalogCandidateSearch
{
    Task<IReadOnlyList<MusicCatalogMatch>> SearchAsync(NormalizedSearchQuery query, CancellationToken cancellationToken);
}
