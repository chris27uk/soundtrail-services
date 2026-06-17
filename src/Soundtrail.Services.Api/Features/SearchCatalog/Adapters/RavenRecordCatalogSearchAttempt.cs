using Raven.Client.Documents;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Api.Features.Search.SearchCatalog.Adapters;

public sealed class RavenRecordCatalogSearchAttempt(
    ICatalogSearchDiscoveryRepository discoveryRepository,
    IQueueCatalogSearchAttemptPort queueCatalogSearchAttemptPort) : IRecordCatalogSearchAttemptPort
{
    public async Task<bool> TryRequestAsync(
        RecordCatalogSearchAttemptCommand command,
        CancellationToken cancellationToken)
    {
        var discovery = await CatalogSearchDiscovery.LoadAsync(discoveryRepository, command.Criteria, cancellationToken);
        if (!discovery.Request(command.Request))
        {
            return false;
        }

        var saved = await discovery.SaveAsync(discoveryRepository, cancellationToken);
        if (!saved)
        {
            return false;
        }

        await queueCatalogSearchAttemptPort.EnqueueAsync(command.Request, cancellationToken);
        return true;
    }
}
