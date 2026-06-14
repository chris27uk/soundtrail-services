using Soundtrail.Domain.Commands;

namespace Soundtrail.Domain.Discovery;

public interface IRequestDiscoveryPort
{
    Task<bool> TryRequestAsync(
        RequestDiscoveryCommand command,
        CancellationToken cancellationToken);
}
