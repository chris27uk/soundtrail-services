using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Features.Search.Contracts;

public interface IResolutionDemandPort
{
    Task<QueryId> RecordDemandAsync(
        NormalizedSearchQuery query,
        CancellationToken cancellationToken);
}
