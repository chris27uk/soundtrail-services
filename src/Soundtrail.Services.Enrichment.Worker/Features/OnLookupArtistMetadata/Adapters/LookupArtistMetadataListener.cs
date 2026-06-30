using Raven.Client.Documents.Session;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Enrichment.Commands;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupArtistMetadata.Adapters;

public sealed class LookupArtistMetadataListener(IHandler<LookupArtistMetadataCommand> handler)
{
    [WolverineHandler]
    [Transactional]
    public Task Handle(
        LookupArtistMetadataCommandDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default) =>
        handler.Handle(TypeTranslationRegistry.Default.ToDomainObject<LookupArtistMetadataCommand>(dto), cancellationToken);
}
