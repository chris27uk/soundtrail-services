using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Events;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnStreamingLocationsRequired.Support;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnStreamingLocationsRequired.Adapters;

public sealed class StreamingLocationsRequiredListener(StreamingLocationsRequiredHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public Task Handle(
        PlaybackReferencesResolutionRequiredMessageDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        return handler.Handle(
            new ScheduleStreamingLocationsLookupCommand(
                MusicCatalogId.From(dto.MusicCatalogId),
                dto.Priority,
                dto.ObservedAt,
                CorrelationId.From(dto.CorrelationId),
                dto.SearchTerm,
                dto.ArtistId,
                dto.AlbumId),
            cancellationToken);
    }
}
