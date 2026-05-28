using MusicResolver.Domain.Tracks;
using MusicResolver.Domain.ValueTypes;

namespace MusicResolver.Application.Ports;

public interface ITrackSearchPort
{
    Task<IReadOnlyList<SearchResult>> SearchAsync(
        NormalizedSearchQuery query,
        Limit limit,
        CancellationToken cancellationToken);

    Task<bool> IsReadyAsync(CancellationToken cancellationToken);
}
