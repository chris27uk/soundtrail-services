using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Messaging.Wolverine;

public sealed class CatalogSearchRequestedListenerWolverineResponsesTests
{
    [Fact]
    public async Task Given_A_Schedulable_Request_When_Handled_Then_Request_And_Candidate_Facts_Are_Stored()
    {
        var env = CatalogSearchRequestedListenerWolverineTestEnvironment.WithASchedulableRequest();
        var criteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);

        await env.HandleSchedulableRequest();

        env.DiscoveryRepository.GetStoredEvents(criteria)
            .Should()
            .Contain(x => x is DiscoveryRequested)
            .And.ContainSingle(x => x is CatalogCandidateIdentified);
    }

    [Fact]
    public async Task Given_A_Request_That_Cannot_Be_Resolved_When_Handled_Then_Request_And_Synthetic_Candidate_Facts_Are_Stored()
    {
        var env = CatalogSearchRequestedListenerWolverineTestEnvironment.WithAnUnschedulableRequest();
        var criteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);

        await env.HandleUnschedulableRequest();

        env.DiscoveryRepository.GetStoredEvents(criteria)
            .Should()
            .Contain(x => x is DiscoveryRequested)
            .And.ContainSingle(x => x is CatalogCandidateIdentified);
    }

    [Fact]
    public async Task Given_A_Resolvable_Request_When_Handled_Then_Only_Request_And_Candidate_Facts_Are_Stored()
    {
        var env = CatalogSearchRequestedListenerWolverineTestEnvironment.WithASchedulableRequest();

        await env.HandleSchedulableRequest();

        env.DiscoveryRepository
            .GetStoredEvents(MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks))
            .Should()
            .OnlyContain(x => x is DiscoveryRequested || x is CatalogCandidateIdentified);
    }
}
