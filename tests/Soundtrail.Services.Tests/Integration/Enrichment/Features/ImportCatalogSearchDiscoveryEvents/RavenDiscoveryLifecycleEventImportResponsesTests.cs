using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Features.ImportCatalogSearchDiscoveryEvents;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class RavenDiscoveryLifecycleEventImportResponsesTests
{
    [Fact]
    public async Task Given_Imported_Discovery_Events_When_Projection_Has_Not_Replayed_Yet_Then_No_Status_Document_Is_Written_Directly()
    {
        await using var env = RavenDiscoveryLifecycleEventImportTestEnvironment.Create();
        var criteria = CatalogSearchCriteria.Search("track", "rare unknown song");

        await env.ImportAsync(
            new ImportCatalogSearchDiscoveryEventsCommand(
                criteria,
                0,
                [new DiscoveryRequested(
                    criteria,
                    NormalizedSearchQuery.FromText("rare unknown song"),
                    1,
                    10,
                    new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero),
                    CorrelationId.From("corr-1"))]));

        var status = await env.LoadStatusAsync(criteria);

        status.Should().BeNull();
    }

    [Fact]
    public async Task Given_Imported_Discovery_Events_When_Projection_Replays_Then_Status_Documents_Are_Built_From_Stored_Events()
    {
        await using var env = RavenDiscoveryLifecycleEventImportTestEnvironment.Create();
        var criteria = CatalogSearchCriteria.Search("track", "rare unknown song");

        await env.ImportAsync(
            new ImportCatalogSearchDiscoveryEventsCommand(
                criteria,
                0,
                [
                    new DiscoveryRequested(
                        criteria,
                        NormalizedSearchQuery.FromText("rare unknown song"),
                        1,
                        10,
                        new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero),
                        CorrelationId.From("corr-1")),
                    new DiscoveryPlanned(
                        criteria,
                        LookupPriorityBand.High,
                        true,
                        30,
                        new DateTimeOffset(2026, 6, 16, 12, 5, 0, TimeSpan.Zero),
                        "Planner queued lookup",
                        new DateTimeOffset(2026, 6, 16, 12, 1, 0, TimeSpan.Zero))
                ]));

        await env.ReplayDiscoveryProjectionAsync();

        var status = await env.LoadStatusAsync(criteria);

        status.Should().NotBeNull();
        status!.Status.Should().Be("Planned");
        status.WillBeLookedUp.Should().BeTrue();
        status.EstimatedRetryAfterSeconds.Should().Be(30);
        status.Reason.Should().Be("Planner queued lookup");
    }
}
