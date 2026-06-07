using Raven.Client.Documents.Session;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Orchestrator.Events;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Execution;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.MusicTracks;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Features.Orchestration;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Infrastructure.Messaging;

public sealed class MusicTrackEventListener(MusicTrackEventCommandHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public object Handle(AppleMusicResolutionRequiredDto dto, IAsyncDocumentSession _) => handler.Handle(
        new AppleMusicResolutionRequired(
            MusicCatalogId.From(dto.MusicCatalogId),
            dto.Priority,
            CorrelationId.From(dto.CorrelationId),
            ProviderName.From(dto.SourceProvider),
            dto.ObservedAt));

    [WolverineHandler]
    [Transactional]
    public object Handle(YouTubeMusicResolutionRequiredDto dto, IAsyncDocumentSession _) => handler.Handle(
        new YouTubeMusicResolutionRequired(
            MusicCatalogId.From(dto.MusicCatalogId),
            dto.Priority,
            CorrelationId.From(dto.CorrelationId),
            ProviderName.From(dto.SourceProvider),
            dto.ObservedAt));
}
