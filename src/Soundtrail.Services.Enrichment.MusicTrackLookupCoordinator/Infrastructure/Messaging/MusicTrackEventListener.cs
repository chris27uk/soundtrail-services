using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Commands;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.Events;
using Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Features.Orchestration;
using Soundtrail.Domain.Events;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Infrastructure.Messaging;

public sealed class MusicTrackEventListener(MusicTrackEventCommandHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public object Handle(AppleMusicResolutionRequiredMessageDto dto, IAsyncDocumentSession _)
    {
        var command = handler.Handle(
            new AppleMusicResolutionRequired(
                MusicCatalogId.From(dto.MusicCatalogId),
                dto.Priority,
                CorrelationId.From(dto.CorrelationId),
                ProviderName.From(dto.SourceProvider),
                dto.ObservedAt));

        return new ResolveApplePlaybackReferenceCommandDto(
            command.CommandId.Value,
            command.MusicCatalogId.Value,
            command.Priority,
            command.CreatedAt,
            command.CorrelationId.Value);
    }

    [WolverineHandler]
    [Transactional]
    public object Handle(YouTubeMusicResolutionRequiredMessageDto dto, IAsyncDocumentSession _)
    {
        var command = handler.Handle(
            new YouTubeMusicResolutionRequired(
                MusicCatalogId.From(dto.MusicCatalogId),
                dto.Priority,
                CorrelationId.From(dto.CorrelationId),
                ProviderName.From(dto.SourceProvider),
                dto.ObservedAt));

        return new ResolveYouTubeMusicPlaybackReferenceCommandDto(
            command.CommandId.Value,
            command.MusicCatalogId.Value,
            command.Priority,
            command.CreatedAt,
            command.CorrelationId.Value);
    }
}
