using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery.Assesment;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnAssessMusicCatalogItem;

public sealed class AssessMusicCatalogItemHandler(
    IEventStreamRepository<, IDomainEvent> discoveryRepository,
    IDiscoveryAssessmentPolicy discoveryPriorityPolicy,
    ILocalMusicTrackSearch localMusicTrackSearch) : IHandler<AssessWorkCommand>
{
    public async Task Handle(AssessWorkCommand command, CancellationToken cancellationToken = default)
    {
        var assessmentResult = 
    }
}

public interface IDiscoveryAssessmentPolicy
{
    DiscoveryAssesment Assess(
        CandidateSearchResponse
        DateTimeOffset createdAt,
        bool isPlayable);
}
