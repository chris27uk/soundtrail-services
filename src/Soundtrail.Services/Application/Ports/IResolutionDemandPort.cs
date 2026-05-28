using Soundtrail.Services.Domain.ValueTypes;

namespace Soundtrail.Services.Application.Ports;

public interface IResolutionDemandPort
{
    Task<QueryId> RecordDemandAsync(
        NormalizedSearchQuery query,
        CancellationToken cancellationToken);
}
