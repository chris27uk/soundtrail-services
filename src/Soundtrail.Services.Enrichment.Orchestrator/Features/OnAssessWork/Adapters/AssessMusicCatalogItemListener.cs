using Raven.Client.Documents.Session;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Discovery.Assesment;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnAssessMusicCatalogItem.Adapters;

public sealed class AssessMusicCatalogItemListener(AssessMusicCatalogItemHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public Task Handle(
        AssessMusicCatalogItemCommandDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default) =>
        handler.Handle(TypeTranslationRegistry.Default.ToDomainObject<AssessWorkCommand>(dto), cancellationToken);
}
