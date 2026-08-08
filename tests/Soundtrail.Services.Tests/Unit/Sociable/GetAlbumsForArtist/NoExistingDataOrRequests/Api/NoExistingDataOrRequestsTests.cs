using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetAlbumsForArtist.NoExistingDataOrRequests.Api;

public sealed class NoExistingDataOrRequestsTests
{
    [Fact]
    public async Task When_Requesting_Then_No_Artist_Albums_Are_Returned()
    {
        var environment = GetAlbumsForArtistSociableTestEnvironment.ForNoExistingDataOrRequests();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        response.Should().BeNull();
    }
}

public sealed class RequestKnownMusicDataMessageEnqueuedTests
{
    [Fact]
    public async Task When_Requesting_Then_The_Operation_Is_Artist_Albums()
    {
        var environment = GetAlbumsForArtistSociableTestEnvironment.ForNoExistingDataOrRequests();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        environment.SentMessage<RequestKnownMusicDataMessage>().Operation
            .Should().BeOfType<CatalogItemOperation.ChildAlbumsForArtist>();
    }

    [Fact]
    public async Task When_Requesting_Then_The_Operation_Contains_The_Artist_Id()
    {
        var environment = GetAlbumsForArtistSociableTestEnvironment.ForNoExistingDataOrRequests();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        ((CatalogItemOperation.ChildAlbumsForArtist)environment.SentMessage<RequestKnownMusicDataMessage>().Operation)
            .Id.Should().Be(environment.ArtistId);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Message_Has_High_Priority()
    {
        var environment = GetAlbumsForArtistSociableTestEnvironment.ForNoExistingDataOrRequests();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        environment.SentMessage<RequestKnownMusicDataMessage>().Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Message_Has_Full_Trust()
    {
        var environment = GetAlbumsForArtistSociableTestEnvironment.ForNoExistingDataOrRequests();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        environment.SentMessage<RequestKnownMusicDataMessage>().TrustLevel.Should().Be(100);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Message_Has_No_Risk()
    {
        var environment = GetAlbumsForArtistSociableTestEnvironment.ForNoExistingDataOrRequests();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        environment.SentMessage<RequestKnownMusicDataMessage>().RiskScore.Should().Be(0);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Message_Time_Is_Set()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 9, 44, 0, TimeSpan.Zero);
        var environment = GetAlbumsForArtistSociableTestEnvironment.ForNoExistingDataOrRequests(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        environment.SentMessage<RequestKnownMusicDataMessage>().RequestedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Message_Id_Is_Set()
    {
        var environment = GetAlbumsForArtistSociableTestEnvironment.ForNoExistingDataOrRequests();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        environment.SentMessage<RequestKnownMusicDataMessage>().Id.Should().NotBe(default(MessageId));
    }

    [Fact]
    public async Task When_Requesting_Then_The_Correlation_Id_Is_Set()
    {
        var environment = GetAlbumsForArtistSociableTestEnvironment.ForNoExistingDataOrRequests();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        environment.SentMessage<RequestKnownMusicDataMessage>().CorrelationId.Should().NotBe(default(CorrelationId));
    }
}
