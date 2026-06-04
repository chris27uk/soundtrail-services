namespace Soundtrail.Services.Features.Search.Queueing;

public interface IEnqueueMusicRequest
{
    Task EnqueueAsync(LookupMusicRequest request, CancellationToken cancellationToken);
}
