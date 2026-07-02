using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnAssessMusicTrack;

public sealed class AssessMusicTrackHandler(
    IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> discoveryRepository,
    IDiscoveryAssessmentPolicy discoveryPriorityPolicy,
    ILocalMusicTrackSearch localMusicTrackSearch) : IHandler<AssessMusicTrackCommand>
{
    public async Task Handle(AssessMusicTrackCommand command, CancellationToken cancellationToken = default)
    {
        if (command.SearchTerm is null)
        {
            throw new InvalidOperationException("Assess music track requires a search criteria.");
        }

        var loaded = await SearchDiscoveryHistory.LoadAsync(
            discoveryRepository,
            command.SearchTerm,
            cancellationToken);

        var localTrack = await localMusicTrackSearch.GetByMusicCatalogIdAsync(command.MusicCatalogId, cancellationToken);
        loaded.Aggregate.Assess(
            discoveryPriorityPolicy,
            command.CreatedAt,
            localTrack?.IsPlayable == true,
            command.MusicCatalogId);

        await loaded.Aggregate.SaveAsync(
            discoveryRepository,
            loaded.Stream,
            cancellationToken);
    }
}
