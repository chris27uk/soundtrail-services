using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForArtist.Scenarios.LookupDataNotComplete.Orchestrator;

public sealed class WorkRequestedEventSavedTests
{
    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Event_Target_Is_A_Known_Catalog_Operation()
    {
        var environment = GetTracksForArtistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        environment.SavedEvent<WorkRequested>().Target.Should().BeOfType<EnrichmentTarget.KnownCatalogItemOperation>();
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Target_Operation_Is_For_Artist_Tracks()
    {
        var environment = GetTracksForArtistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        TargetOperation(environment).Should().BeOfType<CatalogItemOperation.ChildTracksForArtist>();
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Target_Contains_The_Artist_Id()
    {
        var environment = GetTracksForArtistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));
        var operation = (CatalogItemOperation.ChildTracksForArtist)TargetOperation(environment);

        operation.Id.Should().Be(environment.ArtistId);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Target_Has_The_Normalised_Artist_Identifier()
    {
        var environment = GetTracksForArtistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        environment.SavedEvent<WorkRequested>().Target.NormalisedIdentifier
            .Should().Be($"child_tracks_for_artist:{environment.ArtistId.Value}");
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Event_Has_High_Priority()
    {
        var environment = GetTracksForArtistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        environment.SavedEvent<WorkRequested>().Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Event_Has_Full_Trust()
    {
        var environment = GetTracksForArtistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        environment.SavedEvent<WorkRequested>().TrustLevel.Should().Be(100);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Event_Has_No_Risk()
    {
        var environment = GetTracksForArtistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        environment.SavedEvent<WorkRequested>().RiskScore.Should().Be(0);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Event_Request_Time_Is_Saved()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 9, 45, 0, TimeSpan.Zero);
        var environment = GetTracksForArtistSociableTestEnvironment.ForLookupDataNotComplete(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        environment.SavedEvent<WorkRequested>().RequestedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Correlation_Id_Is_Preserved()
    {
        var environment = GetTracksForArtistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        environment.SavedEvent<WorkRequested>().CorrelationId
            .Should().Be(environment.SentMessage<RequestKnownMusicDataMessage>().CorrelationId);
    }

    private static CatalogItemOperation TargetOperation(GetTracksForArtistSociableTestEnvironment environment) =>
        environment.SavedEvent<WorkRequested>().Target switch
        {
            EnrichmentTarget.KnownCatalogItemOperation target => target.Operation,
            _ => throw new InvalidOperationException("The fixed scenario should target a known catalog operation.")
        };
}
