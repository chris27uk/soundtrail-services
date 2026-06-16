using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ImportCatalogSearchDiscoveryEvents;

public sealed class ImportCatalogSearchDiscoveryEventsHandler(
    ICatalogSearchDiscoveryRepository repository) : IHandler<ImportCatalogSearchDiscoveryEventsCommand, ImportCatalogSearchDiscoveryEventsResult>
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

        return new ImportCatalogSearchDiscoveryEventsResult(
            appended,
            appended ? command.Events.Count : 0);
    }
}
