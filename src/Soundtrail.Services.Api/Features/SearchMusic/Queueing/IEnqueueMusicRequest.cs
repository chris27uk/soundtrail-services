using Soundtrail.Domain.Commands;

namespace Soundtrail.Services.Api.Features.SearchMusic.Queueing;

public interface IEnqueueMusicRequest
{
    Task EnqueueAsync(LookupMusicRequest request, CancellationToken cancellationToken);
}
