using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Tests.Unit.Orchestrator.OnKnownMusicDataRequested;

public sealed class KnownCatalogItemRequestsWorkTests
{
    [Fact]
    public async Task Given_A_Request_With_A_Known_Catalog_Item_When_Handling_Then_A_WorkRequested_Event_Is_Appended()
    {
        var environment = OnKnownMusicDataRequestedHandlerUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(OnKnownMusicDataRequestedHandlerUnitTestEnvironment.CreateKnownArtistRequest());

        environment.Repository.AppendedEvents.Should().ContainSingle().Which.Should().BeOfType<WorkRequested>();
    }

    [Fact]
    public async Task Given_A_Request_With_A_Known_Catalog_Item_When_Handling_Then_The_Target_Is_Preserved()
    {
        var environment = OnKnownMusicDataRequestedHandlerUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(OnKnownMusicDataRequestedHandlerUnitTestEnvironment.CreateKnownArtistRequest(artistId: "artist-123"));

        environment.Repository.AppendedEvents
            .Cast<WorkRequested>()
            .Single()
            .Target
            .Should()
            .Be(new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.ChildTracksForArtist(ArtistId.From("artist-123"))));
    }

    [Fact]
    public async Task Given_A_Request_With_A_Known_Catalog_Item_When_Handling_Then_The_Trust_Level_Is_Preserved()
    {
        var environment = OnKnownMusicDataRequestedHandlerUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(OnKnownMusicDataRequestedHandlerUnitTestEnvironment.CreateKnownArtistRequest(trustLevel: 88));

        environment.Repository.AppendedEvents.Cast<WorkRequested>().Single().TrustLevel.Should().Be(88);
    }

    [Fact]
    public async Task Given_A_Request_With_A_Known_Catalog_Item_When_Handling_Then_The_Risk_Score_Is_Preserved()
    {
        var environment = OnKnownMusicDataRequestedHandlerUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(OnKnownMusicDataRequestedHandlerUnitTestEnvironment.CreateKnownArtistRequest(riskScore: 7));

        environment.Repository.AppendedEvents.Cast<WorkRequested>().Single().RiskScore.Should().Be(7);
    }

    [Fact]
    public async Task Given_A_Request_With_A_Known_Catalog_Item_When_Handling_Then_The_Requested_At_Is_Preserved()
    {
        var environment = OnKnownMusicDataRequestedHandlerUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();
        var requestedAt = new DateTimeOffset(2026, 7, 16, 10, 0, 0, TimeSpan.Zero);

        await subject.Handle(OnKnownMusicDataRequestedHandlerUnitTestEnvironment.CreateKnownArtistRequest(requestedAt: requestedAt));

        environment.Repository.AppendedEvents.Cast<WorkRequested>().Single().RequestedAt.Should().Be(requestedAt);
    }

    [Fact]
    public async Task Given_A_Request_With_A_Known_Catalog_Item_When_Handling_Then_The_Correlation_Id_Is_Preserved()
    {
        var environment = OnKnownMusicDataRequestedHandlerUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(OnKnownMusicDataRequestedHandlerUnitTestEnvironment.CreateKnownArtistRequest(correlationId: "corr-known"));

        environment.Repository.AppendedEvents.Cast<WorkRequested>().Single().CorrelationId.Value.Should().Be("corr-known");
    }

    [Fact]
    public async Task Given_A_Request_With_A_Known_Catalog_Item_When_Handling_Then_One_Event_Is_Recorded()
    {
        var environment = OnKnownMusicDataRequestedHandlerUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(OnKnownMusicDataRequestedHandlerUnitTestEnvironment.CreateKnownArtistRequest());

        environment.Repository.AppendedEvents.Should().HaveCount(1);
    }

    [Fact]
    public async Task Given_The_Target_Has_Already_Been_Requested_With_The_Same_Priority_When_Handling_Then_No_New_Event_Is_Appended()
    {
        var environment = OnKnownMusicDataRequestedHandlerUnitTestEnvironment.Create();
        var target = new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.ChildTracksForArtist(ArtistId.From("artist-123")));
        environment.Repository.SeedEvents =
        [
            new WorkRequested(target, LookupPriorityBand.High, 50, 5, new DateTimeOffset(2026, 7, 16, 9, 0, 0, TimeSpan.Zero), CorrelationId.From("corr-old"))
        ];
        var subject = environment.CreateSubject();

        await subject.Handle(OnKnownMusicDataRequestedHandlerUnitTestEnvironment.CreateKnownArtistRequest(priority: LookupPriorityBand.High));

        environment.Repository.AppendedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_The_Target_Has_Already_Been_Requested_At_A_Lower_Priority_When_Handling_Then_A_WorkPriorityRaised_Event_Is_Appended()
    {
        var environment = OnKnownMusicDataRequestedHandlerUnitTestEnvironment.Create();
        var target = new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.ChildTracksForArtist(ArtistId.From("artist-123")));
        environment.Repository.SeedEvents =
        [
            new WorkRequested(target, LookupPriorityBand.Low, 50, 5, new DateTimeOffset(2026, 7, 16, 9, 0, 0, TimeSpan.Zero), CorrelationId.From("corr-old"))
        ];
        var subject = environment.CreateSubject();

        await subject.Handle(OnKnownMusicDataRequestedHandlerUnitTestEnvironment.CreateKnownArtistRequest(priority: LookupPriorityBand.High, trustLevel: 88, riskScore: 7, correlationId: "corr-new"));

        var raised = environment.Repository.AppendedEvents.Should().ContainSingle().Which.Should().BeOfType<WorkPriorityRaised>().Subject;
        raised.Target.Should().Be(target);
        raised.Priority.Should().Be(LookupPriorityBand.High);
        raised.TrustLevel.Should().Be(88);
        raised.RiskScore.Should().Be(7);
        raised.CorrelationId.Value.Should().Be("corr-new");
    }

    [Fact]
    public async Task Given_The_Target_Was_Already_Completed_When_Handling_Then_No_New_Request_Event_Is_Appended()
    {
        var environment = OnKnownMusicDataRequestedHandlerUnitTestEnvironment.Create();
        var target = new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.ChildTracksForArtist(ArtistId.From("artist-123")));
        environment.Repository.SeedEvents =
        [
            new WorkCompleted(target, LookupPriorityBand.High, "done", new DateTimeOffset(2026, 7, 16, 9, 0, 0, TimeSpan.Zero))
        ];
        var subject = environment.CreateSubject();

        await subject.Handle(OnKnownMusicDataRequestedHandlerUnitTestEnvironment.CreateKnownArtistRequest());

        environment.Repository.AppendedEvents.Should().BeEmpty();
    }
}
