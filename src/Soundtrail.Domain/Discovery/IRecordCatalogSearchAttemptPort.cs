using Soundtrail.Domain.Commands;

namespace Soundtrail.Domain.Discovery;

public interface IRecordCatalogSearchAttemptPort
{
    Task<bool> TryRequestAsync(
        RecordCatalogSearchAttemptCommand command,
        CancellationToken cancellationToken);
}
