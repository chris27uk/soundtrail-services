using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Services.Api.Features.SearchCatalog.Ports;

namespace Soundtrail.Services.Api.Features.SearchCatalog.Support;

public sealed class CatalogSearchAttemptRecorder(
    ICatalogSearchDiscoveryRepository discoveryRepository,
    IQueueCatalogSearchAttemptPort queueCatalogSearchAttemptPort)
{
    public async Task<bool> TryRequestAsync(
        RecordCatalogSearchAttemptCommand command,
        CancellationToken cancellationToken)
    {
        var history = await SearchOrSeekHistory.LoadAsync(
            discoveryRepository,
            command.SearchCriteria,
            cancellationToken);
        if (!history.SearchRequested(command.Requested))
        {
            return false;
        }

        var saved = await history.SaveAsync(discoveryRepository, cancellationToken);
        if (!saved)
        {
            return false;
        }

        await queueCatalogSearchAttemptPort.EnqueueAsync(command.Requested, cancellationToken);
        return true;
    }
}
