using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetAlbumsForArtist.Scenarios.LookupDataNotComplete.Projector;

public sealed class AssessWorkMessageEnqueuedTests
{
    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Target_Is_A_Known_Catalog_Operation()
    {
        var environment = GetAlbumsForArtistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        environment.SentMessage<AssessWorkMessage>().Target.Should().BeOfType<EnrichmentTarget.KnownCatalogItemOperation>();
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Target_Is_For_Artist_Albums()
    {
        var environment = GetAlbumsForArtistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        TargetOperation(environment).Should().BeOfType<CatalogItemOperation.ChildAlbumsForArtist>();
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Target_Contains_The_Artist_Id()
    {
        var environment = GetAlbumsForArtistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));
        var operation = (CatalogItemOperation.ChildAlbumsForArtist)TargetOperation(environment);

        operation.Id.Should().Be(environment.ArtistId);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Target_Has_The_Normalised_Identifier()
    {
        var environment = GetAlbumsForArtistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        environment.SentMessage<AssessWorkMessage>().Target.NormalisedIdentifier
            .Should().Be($"child_albums_for_artist:{environment.ArtistId.Value}");
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Has_High_Priority()
    {
        var environment = GetAlbumsForArtistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        environment.SentMessage<AssessWorkMessage>().Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Has_Full_Trust()
    {
        var environment = GetAlbumsForArtistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        environment.SentMessage<AssessWorkMessage>().TrustLevel.Should().Be(100);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Has_No_Risk()
    {
        var environment = GetAlbumsForArtistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        environment.SentMessage<AssessWorkMessage>().RiskScore.Should().Be(0);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 10, 31, 0, TimeSpan.Zero);
        var environment = GetAlbumsForArtistSociableTestEnvironment.ForLookupDataNotComplete(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        environment.SentMessage<AssessWorkMessage>().CreatedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Request_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 10, 32, 0, TimeSpan.Zero);
        var environment = GetAlbumsForArtistSociableTestEnvironment.ForLookupDataNotComplete(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        environment.SentMessage<AssessWorkMessage>().RequestedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Id_Is_Set()
    {
        var environment = GetAlbumsForArtistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        environment.SentMessage<AssessWorkMessage>().Id.Should().NotBe(default(MessageId));
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Correlation_Id_Is_Preserved()
    {
        var environment = GetAlbumsForArtistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        environment.SentMessage<AssessWorkMessage>().CorrelationId
            .Should().Be(environment.SentMessage<RequestKnownMusicDataMessage>().CorrelationId);
    }

    private static CatalogItemOperation TargetOperation(GetAlbumsForArtistSociableTestEnvironment environment) =>
        environment.SentMessage<AssessWorkMessage>().Target switch
        {
            EnrichmentTarget.KnownCatalogItemOperation target => target.Operation,
            _ => throw new InvalidOperationException("The fixed scenario should target a known catalog operation.")
        };
}
