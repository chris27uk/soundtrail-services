using Soundtrail.Contracts.Api;

namespace Soundtrail.Services.Api.Features.Search.Queueing;

public interface IEnqueueMusicRequest
{
    Task EnqueueAsync(LookupMusicRequest request, CancellationToken cancellationToken);
}
