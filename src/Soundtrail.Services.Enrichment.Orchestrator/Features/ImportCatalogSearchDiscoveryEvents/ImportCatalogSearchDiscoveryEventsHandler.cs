using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.ImportCatalogSearchDiscoveryEvents;

public sealed class ImportCatalogSearchDiscoveryEventsHandler(
    ICatalogSearchDiscoveryRepository repository) : IHandler<ImportCatalogSearchDiscoveryEventsCommand>
{
    public async Task Handle(
        ImportCatalogSearchDiscoveryEventsCommand command,
        CancellationToken cancellationToken = default)
    {
        await repository.AppendAsync(
            command.Criteria,
            command.ExpectedVersion,
            command.Events,
            cancellationToken);
    }
}
