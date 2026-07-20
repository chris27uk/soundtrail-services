namespace Soundtrail.Services.Tests.Integration.Ports.LookupPlaylistTracks;

public sealed class PlaylistTracksDoNotExistTests
{
    public static TheoryData<ReadPlaylistTracksByProviderPortImplementation> Implementations => new()
    {
        ReadPlaylistTracksByProviderPortImplementation.Fake,
        ReadPlaylistTracksByProviderPortImplementation.WireMock
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Empty_Playlist_When_Reading_Tracks_Then_No_Rows_Are_Returned(ReadPlaylistTracksByProviderPortImplementation implementation)
    {
        using var environment = ReadPlaylistTracksByProviderPortContractTestEnvironment.ForEmptyPlaylist(implementation);

        var result = await environment.Subject.ReadAsync(environment.PlaylistId, environment.Provider, CancellationToken.None);

        result.Should().BeEmpty();
    }
}
