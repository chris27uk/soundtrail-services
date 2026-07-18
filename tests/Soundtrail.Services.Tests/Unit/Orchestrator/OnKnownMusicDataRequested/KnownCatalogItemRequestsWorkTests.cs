using Soundtrail.Domain.Catalog.Artists;
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
}
