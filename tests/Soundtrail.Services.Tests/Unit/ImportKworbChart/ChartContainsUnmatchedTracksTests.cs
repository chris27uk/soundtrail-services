using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Services.Tests.Unit.ImportKworbChart;

public sealed class ChartContainsUnmatchedTracksTests
{
    [Fact]
    public async Task Given_A_Chart_With_An_Unmatched_Track_When_Importing_The_Kworb_Chart_Then_The_Unmatched_Track_Is_Not_Sent()
    {
        var environment = ImportKworbChartUnitTestEnvironment.ForChart(
            chartRows: ImportKworbChartTracks.CreateChartRows(
                ("Artist 1", "Track 1"),
                ("Artist 2", "Track 2")));

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().Tracks.Should().Equal(TrackId.From("track-1701"));
    }

    [Fact]
    public async Task Given_A_Chart_With_An_Invalid_Row_When_Importing_The_Kworb_Chart_Then_The_Invalid_Row_Is_Not_Read_By_Fingerprint()
    {
        var environment = ImportKworbChartUnitTestEnvironment.ForChart(chartRows: []);

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.LoadTrackByFingerprintPort.RequestedFingerprints.Should().BeEmpty();
    }
}
