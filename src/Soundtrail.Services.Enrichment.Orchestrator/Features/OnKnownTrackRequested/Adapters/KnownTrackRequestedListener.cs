using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownTrackRequested.Adapters;

public sealed class KnownTrackRequestedListener(KnownTrackRequestedHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public Task Handle(
        KnownTrackRequestedDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default) =>
        handler.Handle(
            new KnownTrackRequested(
                TrackId.From(dto.TrackId),
                PlaybackProviderFilter.Parse(dto.Playback),
                dto.OccurredAt,
                CorrelationId.From(dto.CorrelationId)),
            cancellationToken);
}
