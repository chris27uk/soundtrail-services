using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;

namespace Soundtrail.Services.Tests.Unit.Worker.LookupPlaylistTracks;

public sealed class LookupPlaylistTracksByProviderHandlerTests
{
    [Fact]
    public async Task Given_Playlist_Tracks_When_Handling_Then_A_Playlist_Track_References_Result_Is_Published()
    {
        var environment = LookupPlaylistTracksUnitTestEnvironment.Create();
        environment.ReadPlaylistTracksByProviderPort.Result = LookupPlaylistTracksUnitTestEnvironment.CreateTrackReferences(
            ("U2", "Street Of Dreams"),
            ("HUGEL, Imael Angel & Ultra Nate", "Movin' To The Sun"));
        var subject = environment.CreateBusinessSubject();
        var request = environment.CreateRequest();

        await subject.Handle(request);

        var message = environment.CommandBus.Messages.Single().Should().BeOfType<CatalogLookupCompleted>().Subject;
        message.RequestedAt.Should().Be(request.RequestedAt);
        message.CorrelationId.Should().Be(request.CorrelationId);
        var succeeded = message.Result.Should().BeOfType<LookupResult.Succeeded>().Subject;
        succeeded.Context.OriginalCommandId.Should().Be(request.Id);
        succeeded.CompletedAt.Should().Be(environment.Clock.UtcNow);
        var references = succeeded.Value.Should().BeOfType<LookedUpData.PlaylistTrackReferences>().Subject;
        references.Values.Select(x => (x.ArtistName.Value, x.TrackTitle)).Should().Equal(
            ("U2", "Street Of Dreams"),
            ("HUGEL, Imael Angel & Ultra Nate", "Movin' To The Sun"));
    }

    [Fact]
    public async Task Given_A_Playlist_Request_When_Handling_Then_The_Port_Is_Called_With_The_Request_Inputs()
    {
        var environment = LookupPlaylistTracksUnitTestEnvironment.Create();
        var subject = environment.CreateBusinessSubject();
        var request = environment.CreateRequest();

        await subject.Handle(request);

        environment.ReadPlaylistTracksByProviderPort.RequestedPlaylistId.Should().Be(request.PlaylistId);
        environment.ReadPlaylistTracksByProviderPort.RequestedProvider.Should().Be(request.Provider);
    }
}
