namespace Soundtrail.Domain.Commands;

public interface IQueueLookupMusicRequestPort
{
    Task EnqueueAsync(LookupMusicRequest request, CancellationToken cancellationToken);
}
