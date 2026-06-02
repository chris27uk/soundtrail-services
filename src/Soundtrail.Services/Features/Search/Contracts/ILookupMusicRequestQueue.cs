using Soundtrail.Services.Features.Search.Queueing;

namespace Soundtrail.Services.Features.Search.Contracts;

public interface ILookupMusicRequestQueue
{
    Task EnqueueAsync(
        LookupMusicRequest request,
        CancellationToken cancellationToken);
}
