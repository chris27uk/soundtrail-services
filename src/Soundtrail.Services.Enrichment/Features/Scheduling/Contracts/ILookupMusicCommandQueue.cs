using Soundtrail.Services.Enrichment.Features.Scheduling.Models;

namespace Soundtrail.Services.Enrichment.Features.Scheduling.Contracts;

public interface ILookupMusicCommandQueue
{
    Task EnqueueAsync(LookupMusicCommand command, CancellationToken cancellationToken);
}
