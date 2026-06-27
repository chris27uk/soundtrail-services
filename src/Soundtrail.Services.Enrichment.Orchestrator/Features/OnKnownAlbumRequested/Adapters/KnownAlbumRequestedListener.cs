using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery.Commands;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested.Adapters;

public sealed class KnownAlbumRequestedListener(KnownAlbumRequestedHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public Task Handle(
        KnownAlbumRequestedDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default) =>
        handler.Handle(
            new KnownAlbumRequested(
                AlbumId.From(dto.AlbumId),
                dto.OccurredAt,
                CorrelationId.From(dto.CorrelationId)),
            cancellationToken);
}
