using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Api.Features.SearchMusic.TrackSearch;

public interface ITrackSearchPort
{
    Task<IReadOnlyList<SearchResult>> SearchAsync(
        NormalizedSearchQuery query,
        Limit limit,
        CancellationToken cancellationToken);

    Task<bool> IsReadyAsync(CancellationToken cancellationToken);
}
