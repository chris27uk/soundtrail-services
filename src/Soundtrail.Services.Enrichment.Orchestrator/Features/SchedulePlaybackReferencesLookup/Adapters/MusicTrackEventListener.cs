using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Events;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.SchedulePlaybackReferencesLookup.Adapters;

public sealed class MusicTrackEventListener(SchedulePlaybackReferencesLookupHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public Task Handle(
        PlaybackReferencesResolutionRequiredMessageDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        return handler.Handle(
            new SchedulePlaybackReferencesLookupCommand(
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
