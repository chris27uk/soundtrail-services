namespace Soundtrail.Services.Tests.Integration.Ports.ImportKworbChart;

public sealed class KworbChartDoesNotExistTests
{
    public static TheoryData<ReadKworbChartPortImplementation> Implementations => new()
    {
        ReadKworbChartPortImplementation.Fake,
        ReadKworbChartPortImplementation.WireMock
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Empty_Kworb_Chart_When_Reading_The_Chart_Then_No_Rows_Are_Returned(ReadKworbChartPortImplementation implementation)
    {
        using var environment = ReadKworbChartPortContractTestEnvironment.ForEmptyChart(implementation);

        var result = await environment.Subject.ReadAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }
}
