using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ImportCatalogSearchDiscoveryEvents;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.ImportCatalogSearchDiscoveryEvents;

public sealed class ImportCatalogSearchDiscoveryEventsHandlerTests
{
    [Fact]
    public async Task Given_Discovery_Lifecycle_Events_When_Imported_Then_They_Are_Appended_To_The_Event_Store()
    {
        var repository = new CatalogSearchDiscoveryRepositoryFake();
        var handler = new ImportCatalogSearchDiscoveryEventsHandler(repository);
        var criteria = CatalogSearchCriteria.Search("track", "rare unknown song");
        var command = new ImportCatalogSearchDiscoveryEventsCommand(
            criteria,
            0,
            [new DiscoveryRequested(
                criteria,
                NormalizedSearchQuery.FromText("rare unknown song"),
                1,
                10,
                new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero),
                CorrelationId.From("corr-1"))]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Appended.Should().BeTrue();
        result.ImportedEventCount.Should().Be(1);
        repository.GetStoredEvents(criteria).Should().ContainSingle().Which.Should().BeOfType<DiscoveryRequested>();
    }
}
