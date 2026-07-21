using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;

namespace Soundtrail.Services.Tests.Unit.Worker.LookupMusicbrainzBrowse;

public sealed class LookupMusicbrainzBrowseHandlerTests
{
    [Fact]
    public async Task Given_Artist_Albums_When_Handling_Then_A_Succeeded_Result_Is_Published()
    {
        var environment = LookupMusicbrainzBrowseUnitTestEnvironment.Create();
        var request = environment.CreateArtistAlbumsRequest();
        var subject = environment.CreateArtistAlbumsBusinessSubject();

        await subject.Handle(request, CancellationToken.None);

        environment.ReadAlbumsByArtistIdPort.RequestedArtistId.Should().Be(request.ArtistId);
        environment.CommandBus.Messages.Single()
            .Should().BeOfType<CatalogLookupCompleted>().Subject.Result
            .Should().BeOfType<LookupResult.Succeeded>();
    }

    [Fact]
    public async Task Given_Artist_Tracks_When_Handling_Then_A_Succeeded_Result_Is_Published()
    {
        var environment = LookupMusicbrainzBrowseUnitTestEnvironment.Create();
        var request = environment.CreateArtistTracksRequest();
        var subject = environment.CreateArtistTracksBusinessSubject();

        await subject.Handle(request, CancellationToken.None);

        environment.ReadTracksByArtistIdPort.RequestedArtistId.Should().Be(request.ArtistId);
        environment.CommandBus.Messages.Single()
            .Should().BeOfType<CatalogLookupCompleted>().Subject.Result
            .Should().BeOfType<LookupResult.Succeeded>();
    }

    [Fact]
    public async Task Given_Album_Tracks_When_Handling_Then_A_Succeeded_Result_Is_Published()
    {
        var environment = LookupMusicbrainzBrowseUnitTestEnvironment.Create();
        var request = environment.CreateAlbumTracksRequest();
        var subject = environment.CreateAlbumTracksBusinessSubject();

        await subject.Handle(request, CancellationToken.None);

        environment.ReadTracksByAlbumIdPort.RequestedAlbumId.Should().Be(request.AlbumId);
        environment.CommandBus.Messages.Single()
            .Should().BeOfType<CatalogLookupCompleted>().Subject.Result
            .Should().BeOfType<LookupResult.Succeeded>();
    }
}
