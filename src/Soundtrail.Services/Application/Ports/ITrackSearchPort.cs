using Soundtrail.Services.Domain.Tracks;
using Soundtrail.Services.Domain.ValueTypes;

namespace Soundtrail.Services.Application.Ports;

public interface ITrackSearchPort
{
    Task<IReadOnlyList<SearchResult>> SearchAsync(
        NormalizedSearchQuery query,
        Limit limit,
        CancellationToken cancellationToken);

    Task<bool> IsReadyAsync(CancellationToken cancellationToken);
}
