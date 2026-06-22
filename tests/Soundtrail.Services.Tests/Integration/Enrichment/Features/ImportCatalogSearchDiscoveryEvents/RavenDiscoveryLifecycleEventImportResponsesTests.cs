using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
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
    public async Task Given_Imported_Discovery_Events_When_Import_Completes_Then_Status_Documents_Are_Built_By_The_Runtime_Projector()
    {
        await using var env = await RavenDiscoveryLifecycleEventImportTestEnvironment.CreateAsync();
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

        var status = await env.WaitForStatusAsync(criteria, TimeSpan.FromSeconds(10));

        status.Should().NotBeNull();
        status!.Status.Should().Be("Requested");
        status.WillBeLookedUp.Should().BeTrue();
    }

    [Fact]
    public async Task Given_Imported_Discovery_Events_When_Import_Completes_Then_Status_Documents_Are_Built_By_The_Subscription_Projector()
    {
        await using var env = await RavenDiscoveryLifecycleEventImportTestEnvironment.CreateAsync();
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

        var status = await env.WaitForStatusAsync(criteria, TimeSpan.FromSeconds(10));

        status.Should().NotBeNull();
        status!.Status.Should().Be("Planned");
        status.WillBeLookedUp.Should().BeTrue();
        status.EstimatedRetryAfterSeconds.Should().Be(30);
        status.Reason.Should().Be("Planner queued lookup");
    }

    [Fact]
    public async Task Given_Imported_Album_Criteria_Discovery_Events_When_Import_Completes_Then_The_Album_Status_Document_Is_Built()
    {
        await using var env = await RavenDiscoveryLifecycleEventImportTestEnvironment.CreateAsync();
        var criteria = CatalogSearchCriteria.Album(AlbumId.From("album_test_album"));

        await env.ImportAsync(
            new ImportCatalogSearchDiscoveryEventsCommand(
                criteria,
                0,
                [
                    new DiscoveryRequested(
                        criteria,
                        NormalizedSearchQuery.FromText("test album"),
                        1,
                        10,
                        new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero),
                        CorrelationId.From("corr-album")),
                    new DiscoveryPlanned(
                        criteria,
                        LookupPriorityBand.High,
                        true,
                        60,
                        new DateTimeOffset(2026, 6, 16, 12, 6, 0, TimeSpan.Zero),
                        "Planner queued album lookup",
                        new DateTimeOffset(2026, 6, 16, 12, 1, 0, TimeSpan.Zero))
                ]));

        var status = await env.WaitForStatusAsync(criteria, TimeSpan.FromSeconds(10));

        status.Should().NotBeNull();
        status!.Status.Should().Be("Planned");
        status.WillBeLookedUp.Should().BeTrue();
        status.EstimatedRetryAfterSeconds.Should().Be(60);
        status.Reason.Should().Be("Planner queued album lookup");
    }

    [Fact]
    public async Task Given_Imported_Track_Criteria_Discovery_Events_When_Import_Completes_Then_The_Track_Status_Document_Is_Built()
    {
        await using var env = await RavenDiscoveryLifecycleEventImportTestEnvironment.CreateAsync();
        var criteria = CatalogSearchCriteria.Track(TrackId.From("mc_track_1"));

        await env.ImportAsync(
            new ImportCatalogSearchDiscoveryEventsCommand(
                criteria,
                0,
                [
                    new DiscoveryStarted(
                        criteria,
                        LookupPriorityBand.High,
                        true,
                        "Track lookup started",
                        new DateTimeOffset(2026, 6, 16, 12, 2, 0, TimeSpan.Zero))
                ]));

        var status = await env.WaitForStatusAsync(criteria, TimeSpan.FromSeconds(10));

        status.Should().NotBeNull();
        status!.Status.Should().Be("InProgress");
        status.WillBeLookedUp.Should().BeTrue();
        status.Reason.Should().Be("Track lookup started");
    }
}
