using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForAlbum.Scenarios.LookupDataNotComplete.Projector;

public sealed class AssessWorkMessageEnqueuedTests
{
    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Target_Is_A_Known_Catalog_Operation()
    {
        var environment = GetTracksForAlbumSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        environment.SentMessage<AssessWorkMessage>().Target.Should().BeOfType<EnrichmentTarget.KnownCatalogItemOperation>();
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Target_Is_For_Album_Tracks()
    {
        var environment = GetTracksForAlbumSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        TargetOperation(environment).Should().BeOfType<CatalogItemOperation.ChildTracksForAlbum>();
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Target_Contains_The_Album_Id()
    {
        var environment = GetTracksForAlbumSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));
        var operation = (CatalogItemOperation.ChildTracksForAlbum)TargetOperation(environment);

        operation.Id.Should().Be(environment.AlbumId);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Target_Has_The_Normalised_Identifier()
    {
        var environment = GetTracksForAlbumSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        environment.SentMessage<AssessWorkMessage>().Target.NormalisedIdentifier
            .Should().Be($"child_tracks_for_album:{environment.AlbumId.StableValue}");
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Has_High_Priority()
    {
        var environment = GetTracksForAlbumSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        environment.SentMessage<AssessWorkMessage>().Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Has_Full_Trust()
    {
        var environment = GetTracksForAlbumSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        environment.SentMessage<AssessWorkMessage>().TrustLevel.Should().Be(100);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Has_No_Risk()
    {
        var environment = GetTracksForAlbumSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        environment.SentMessage<AssessWorkMessage>().RiskScore.Should().Be(0);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 10, 31, 0, TimeSpan.Zero);
        var environment = GetTracksForAlbumSociableTestEnvironment.ForLookupDataNotComplete(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        environment.SentMessage<AssessWorkMessage>().CreatedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Request_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 10, 32, 0, TimeSpan.Zero);
        var environment = GetTracksForAlbumSociableTestEnvironment.ForLookupDataNotComplete(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        environment.SentMessage<AssessWorkMessage>().RequestedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Id_Is_Set()
    {
        var environment = GetTracksForAlbumSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        environment.SentMessage<AssessWorkMessage>().Id.Should().NotBe(default(MessageId));
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Correlation_Id_Is_Preserved()
    {
        var environment = GetTracksForAlbumSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        environment.SentMessage<AssessWorkMessage>().CorrelationId
            .Should().Be(environment.SentMessage<RequestKnownMusicDataMessage>().CorrelationId);
    }

    private static CatalogItemOperation TargetOperation(GetTracksForAlbumSociableTestEnvironment environment) =>
        environment.SentMessage<AssessWorkMessage>().Target switch
        {
            EnrichmentTarget.KnownCatalogItemOperation target => target.Operation,
            _ => throw new InvalidOperationException("The fixed scenario should target a known catalog operation.")
        };
}
