using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ReplayDiscoveryLifecycleProjection;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ImportCatalogSearchDiscoveryEvents;

public sealed class ImportCatalogSearchDiscoveryEventsHandler(
    ICatalogSearchDiscoveryRepository repository,
    ReplayDiscoveryLifecycleProjectionHandler replayHandler) : IHandler<ImportCatalogSearchDiscoveryEventsCommand, ImportCatalogSearchDiscoveryEventsResult>
{
    public async Task<ImportCatalogSearchDiscoveryEventsResult> Handle(
        ImportCatalogSearchDiscoveryEventsCommand command,
        CancellationToken cancellationToken = default)
    {
        var appended = await repository.AppendAsync(
            command.Criteria,
            command.ExpectedVersion,
            command.Events,
            cancellationToken);

        if (appended)
        {
            await replayHandler.Handle(
                new ReplayDiscoveryLifecycleProjectionCommand(command.Criteria),
                cancellationToken);
        }

        return new ImportCatalogSearchDiscoveryEventsResult(
            appended,
            appended ? command.Events.Count : 0);
    }
}
