using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery.Commands;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested.Adapters;

public sealed class KnownArtistRequestedListener(KnownArtistRequestedHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public Task Handle(
        KnownArtistRequestedDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default) =>
        handler.Handle(
            new KnownArtistRequested(
                ArtistId.From(dto.ArtistId),
                dto.OccurredAt,
                CorrelationId.From(dto.CorrelationId)),
            cancellationToken);
}
