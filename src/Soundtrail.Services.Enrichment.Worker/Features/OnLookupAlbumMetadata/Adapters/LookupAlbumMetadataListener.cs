using Raven.Client.Documents.Session;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Abstractions; 
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupAlbumMetadata.Adapters;

public sealed class LookupAlbumMetadataListener(IHandler<LookupAlbumCommand> handler)
{
    [WolverineHandler]
    [Transactional]
    public Task Handle(
        LookupAlbumMetadataCommandDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default) =>
        handler.Handle(TypeTranslationRegistry.Default.ToDomainObject<LookupAlbumCommand>(dto), cancellationToken);
}
