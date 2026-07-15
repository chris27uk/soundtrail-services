namespace Soundtrail.Services.Tests.Integration.Ports.ImportKworbChart;

public sealed class KworbChartExistsTests
{
    public static TheoryData<ReadKworbChartPortImplementation> Implementations => new()
    {
        ReadKworbChartPortImplementation.Fake,
        ReadKworbChartPortImplementation.WireMock
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_A_Kworb_Chart_When_Reading_The_Chart_Then_Tracks_Are_Returned(ReadKworbChartPortImplementation implementation)
    {
        using var environment = ReadKworbChartPortContractTestEnvironment.ForExistingChart(implementation);

        var result = await environment.Subject.ReadAsync(CancellationToken.None);

        result.Should().NotBeEmpty();
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_A_Kworb_Chart_When_Reading_The_Chart_Then_The_Artist_Name_Is_Returned(ReadKworbChartPortImplementation implementation)
    {
        using var environment = ReadKworbChartPortContractTestEnvironment.ForExistingChart(implementation);

        var result = await environment.Subject.ReadAsync(CancellationToken.None);

        result.Single().ArtistName.Value.Should().Be("Artist 1901");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_A_Kworb_Chart_When_Reading_The_Chart_Then_The_Track_Title_Is_Returned(ReadKworbChartPortImplementation implementation)
    {
        using var environment = ReadKworbChartPortContractTestEnvironment.ForExistingChart(implementation);

        var result = await environment.Subject.ReadAsync(CancellationToken.None);

        result.Single().TrackTitle.Should().Be("Track 1901");
    }

    [Fact]
    public async Task Given_The_Kworb_Worldwide_Chart_Html_When_Reading_The_Chart_Then_The_First_Artist_Name_Is_Returned()
    {
        using var environment = ReadKworbChartPortContractTestEnvironment.ForWorldwideChartFixture();

        var result = await environment.Subject.ReadAsync(CancellationToken.None);

        result.First().ArtistName.Value.Should().Be("U2");
    }

    [Fact]
    public async Task Given_The_Kworb_Worldwide_Chart_Html_When_Reading_The_Chart_Then_The_First_Track_Title_Is_Returned()
    {
        using var environment = ReadKworbChartPortContractTestEnvironment.ForWorldwideChartFixture();

        var result = await environment.Subject.ReadAsync(CancellationToken.None);

        result.First().TrackTitle.Should().Be("Street Of Dreams");
    }

    [Fact]
    public async Task Given_The_Kworb_Worldwide_Chart_Html_When_Reading_The_Chart_Then_The_Second_Artist_Name_Is_Returned()
    {
        using var environment = ReadKworbChartPortContractTestEnvironment.ForWorldwideChartFixture();

        var result = await environment.Subject.ReadAsync(CancellationToken.None);

        result.Skip(1).First().ArtistName.Value.Should().Be("HUGEL, Imael Angel & Ultra Naté");
    }

    [Fact]
    public async Task Given_The_Kworb_Worldwide_Chart_Html_When_Reading_The_Chart_Then_The_Second_Track_Title_Is_Returned()
    {
        using var environment = ReadKworbChartPortContractTestEnvironment.ForWorldwideChartFixture();

        var result = await environment.Subject.ReadAsync(CancellationToken.None);

        result.Skip(1).First().TrackTitle.Should().Be("Movin' To The Sun");
    }

    [Fact]
    public async Task Given_The_Kworb_Worldwide_Chart_Html_When_Reading_The_Chart_Then_Tracks_Are_Ordered_By_Position()
    {
        using var environment = ReadKworbChartPortContractTestEnvironment.ForWorldwideChartFixture();

        var result = await environment.Subject.ReadAsync(CancellationToken.None);

        result.Take(3).Select(track => track.TrackTitle).Should().Equal(
            "Street Of Dreams",
            "Movin' To The Sun",
            "Dai Dai");
    }
}
