using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Events;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Infrastructure.Messaging;

public sealed class MusicTrackEventListener
{
    [WolverineHandler]
    [Transactional]
    public object Handle(PlaybackReferencesResolutionRequiredMessageDto dto, IAsyncDocumentSession _)
    {
        return new ResolvePlaybackReferencesCommandDto(
            CommandId.For($"ResolvePlaybackReferences:{dto.MusicCatalogId}").Value,
            dto.MusicCatalogId,
            dto.Priority,
            dto.ObservedAt,
            dto.CorrelationId,
            dto.SearchTerm,
            dto.ArtistId,
            dto.AlbumId);
    }
}
