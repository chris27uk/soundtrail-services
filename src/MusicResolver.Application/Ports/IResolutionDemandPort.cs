using MusicResolver.Domain.ValueTypes;

namespace MusicResolver.Application.Ports;

public interface IResolutionDemandPort
{
    Task<QueryId> RecordDemandAsync(
        NormalizedSearchQuery query,
        CancellationToken cancellationToken);
}
