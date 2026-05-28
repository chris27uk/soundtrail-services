using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Features.Search.Contracts;

public interface ITrackSearchPort
{
    Task<IReadOnlyList<SearchResult>> SearchAsync(
        NormalizedSearchQuery query,
        Limit limit,
        CancellationToken cancellationToken);

    Task<bool> IsReadyAsync(CancellationToken cancellationToken);
}
