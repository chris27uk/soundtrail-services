using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Tests.Unit.ImportKworbChart;

public sealed class ChartContainsMatchingTracksTests
{
    [Fact]
    public async Task Given_A_Chart_With_Matching_Tracks_When_Importing_The_Kworb_Chart_Then_A_Playlist_Update_Is_Sent()
    {
        var environment = ImportKworbChartUnitTestEnvironment.ForChart();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_A_Chart_With_Matching_Tracks_When_Importing_The_Kworb_Chart_Then_The_Playlist_Name_Is_Worldwide_Song_Chart()
    {
        var environment = ImportKworbChartUnitTestEnvironment.ForChart();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().Name.Should().Be("WorldwideSongChart");
    }

    [Fact]
    public async Task Given_A_Chart_With_Matching_Tracks_When_Importing_The_Kworb_Chart_Then_The_Tracks_Are_Sent()
    {
        var environment = ImportKworbChartUnitTestEnvironment.ForChart();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().Tracks.Should().HaveCount(1);
    }

    [Fact]
    public async Task Given_A_Chart_With_Matching_Tracks_When_Importing_The_Kworb_Chart_Then_The_First_Track_Is_Sent()
    {
        var environment = ImportKworbChartUnitTestEnvironment.ForChart();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().Tracks.Single().Should().Be(TrackId.From("track-1701"));
    }

    [Fact]
    public async Task Given_A_Chart_With_Matching_Tracks_When_Importing_The_Kworb_Chart_Then_The_Artist_And_Title_Are_Converted_To_A_Fingerprint()
    {
        var environment = ImportKworbChartUnitTestEnvironment.ForChart();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.LoadTrackByFingerprintPort.RequestedFingerprints.Single().Should().Be(
            TrackMatchFingerprint.FromArtistAndTitle("Artist 1", "Track 1"));
    }

    [Fact]
    public async Task Given_A_Chart_With_Matching_Tracks_When_Importing_The_Kworb_Chart_Then_The_Tracks_Are_Sorted_By_Position()
    {
        var environment = ImportKworbChartUnitTestEnvironment.ForChart(
            chartRows: ImportKworbChartTracks.CreateChartRows(
                ("Artist 2", "Track 2"),
                ("Artist 1", "Track 1")),
            trackIdsByFingerprint: new Dictionary<TrackMatchFingerprint, TrackId>
            {
                [ImportKworbChartTracks.Fingerprint("Artist 1", "Track 1")] = TrackId.From("track-1701"),
                [ImportKworbChartTracks.Fingerprint("Artist 2", "Track 2")] = TrackId.From("track-1702")
            });

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().Tracks.Should().Equal(TrackId.From("track-1702"), TrackId.From("track-1701"));
    }
}
