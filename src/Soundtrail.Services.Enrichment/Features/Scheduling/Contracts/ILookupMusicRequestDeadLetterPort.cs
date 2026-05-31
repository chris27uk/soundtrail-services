using Soundtrail.Services.Enrichment.Features.Scheduling.Models;

namespace Soundtrail.Services.Enrichment.Features.Scheduling.Contracts;

public interface ILookupMusicRequestDeadLetterPort
{
    Task DeadLetterAsync(
        LookupMusicRequest request,
        string reason,
        CancellationToken cancellationToken);
}
