using FluentAssertions;

namespace Soundtrail.Services.Tests.Integration.MusicBrainzImport.Ports.MusicBrainzDumpReader;

public sealed class MusicBrainzJsonDumpReaderResponsesTests
{
    [Theory]
    [MemberData(nameof(MusicBrainzDumpReaderContractModes.All), MemberType = typeof(MusicBrainzDumpReaderContractModes))]
    public async Task Given_A_Recording_Dump_When_Read_Then_Standalone_Recordings_Are_Returned(MusicBrainzDumpReaderMode mode)
    {
        using var env = MusicBrainzDumpReaderTestEnvironment.Create(mode);

        var results = await env.ReadAsync();

        results.Should().ContainEquivalentOf(new
        {
            SourceRecordKey = "recording:mb-recording-1",
            SourceTrackId = "mb-recording-1",
            Title = "Standalone Song",
            Artist = "Standalone Artist",
            SourceArtistId = "mb-artist-standalone",
            AlbumTitle = "Standalone Album",
            SourceAlbumId = "mb-release-standalone",
            Isrc = "USRC17607839",
            MusicBrainzRecordingId = "mb-recording-1",
            DurationMs = 180000,
            ReleaseDate = new DateOnly(1976, 1, 1)
        });
    }

    [Theory]
    [MemberData(nameof(MusicBrainzDumpReaderContractModes.All), MemberType = typeof(MusicBrainzDumpReaderContractModes))]
    public async Task Given_A_Release_Dump_When_Read_Then_Release_Tracks_Are_Returned(MusicBrainzDumpReaderMode mode)
    {
        using var env = MusicBrainzDumpReaderTestEnvironment.Create(mode);

        var results = await env.ReadAsync();

        results.Should().ContainEquivalentOf(new
        {
            SourceRecordKey = "release:mb-release-1:medium:1:track:track-1",
            SourceTrackId = "mb-recording-2",
            Title = "Release Song",
            Artist = "Release Artist",
            SourceArtistId = "mb-artist-release",
            AlbumTitle = "Release Album",
            SourceAlbumId = "mb-release-1",
            Isrc = "USIR20400274",
            MusicBrainzRecordingId = "mb-recording-2",
            DurationMs = 222000,
            ReleaseDate = new DateOnly(2004, 6, 7)
        });
    }
}
