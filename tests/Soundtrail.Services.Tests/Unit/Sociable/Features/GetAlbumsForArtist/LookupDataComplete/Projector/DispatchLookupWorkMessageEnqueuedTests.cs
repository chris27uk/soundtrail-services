using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetAlbumsForArtist.LookupDataComplete.Projector;

public sealed class DispatchLookupWorkMessageEnqueuedTests
{
    [Fact]
    public async Task Then_The_Target_Is_A_Known_Catalog_Operation()
    {
        var environment = ForCompletedAlbum();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Message(environment).Target.Should().BeOfType<EnrichmentTarget.KnownCatalogItemOperation>();
    }

    [Fact]
    public async Task Then_The_Target_Operation_Is_Artist_Albums()
    {
        var environment = ForCompletedAlbum();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Operation(environment).Should().BeOfType<CatalogItemOperation.ChildAlbumsForArtist>();
    }

    [Fact]
    public async Task Then_The_Target_Contains_The_Artist_Id()
    {
        var environment = ForCompletedAlbum();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        ((CatalogItemOperation.ChildAlbumsForArtist)Operation(environment)).Id.Should().Be(environment.ArtistId);
    }

    [Fact]
    public async Task Then_The_Target_Normalised_Identifier_Contains_The_Artist_Id()
    {
        var environment = ForCompletedAlbum();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Message(environment).Target.NormalisedIdentifier
            .Should().Be($"child_albums_for_artist:{environment.ArtistId.Value}");
    }

    [Fact]
    public async Task Then_The_Message_Has_High_Priority()
    {
        var environment = ForCompletedAlbum();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Message(environment).Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Then_The_Created_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 31, 0, TimeSpan.Zero);
        var environment = ForCompletedAlbum(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Message(environment).CreatedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Then_The_Request_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 32, 0, TimeSpan.Zero);
        var environment = ForCompletedAlbum(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Message(environment).RequestedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Then_The_Message_Id_Is_Set()
    {
        var environment = ForCompletedAlbum();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Message(environment).Id.Should().NotBe(default(MessageId));
    }

    [Fact]
    public async Task Then_The_Correlation_Id_Is_Preserved()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 33, 0, TimeSpan.Zero);
        var environment = ForCompletedAlbum(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Message(environment).CorrelationId.Value
            .Should().Be($"work-scheduled:child_albums_for_artist:{environment.ArtistId.Value}:{requestTime:O}");
    }

    private static GetAlbumsForArtistSociableTestEnvironment ForCompletedAlbum(DateTimeOffset requestTime = default) =>
        GetAlbumsForArtistSociableTestEnvironment.ForLookupDataComplete(
            requestTime,
            LookupDataCompleteArtistAlbumScenarios.MidnightSignals(requestTime));

    private static DispatchLookupWork Message(GetAlbumsForArtistSociableTestEnvironment environment) =>
        environment.SentMessages<DispatchLookupWork>()
            .Single(message => message.Target.NormalisedIdentifier == $"child_albums_for_artist:{environment.ArtistId.Value}");

    private static CatalogItemOperation Operation(GetAlbumsForArtistSociableTestEnvironment environment) =>
        ((EnrichmentTarget.KnownCatalogItemOperation)Message(environment).Target).Operation;
}
