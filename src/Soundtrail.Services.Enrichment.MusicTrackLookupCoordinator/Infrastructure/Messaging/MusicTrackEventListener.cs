using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Commands;
using Soundtrail.Contracts.Events;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Infrastructure.Messaging;

public sealed class MusicTrackEventListener
{
    [WolverineHandler]
    [Transactional]
    public object Handle(PlaybackReferencesResolutionRequiredMessageDto dto, IAsyncDocumentSession _)
    {
        return new ResolvePlaybackReferencesCommandDto(
            Soundtrail.Contracts.Common.CommandId.For($"ResolvePlaybackReferences:{dto.MusicCatalogId}").Value,
            dto.MusicCatalogId,
            dto.Priority,
            dto.ObservedAt,
            dto.CorrelationId,
            dto.LookupKey);
    }
}
