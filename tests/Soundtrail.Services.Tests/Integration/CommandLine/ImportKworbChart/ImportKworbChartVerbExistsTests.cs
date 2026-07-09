namespace Soundtrail.Services.Tests.Integration.CommandLine.ImportKworbChart;

public sealed class ImportKworbChartVerbExistsTests
{
    [Fact]
    public async Task Given_The_Import_Kworb_Chart_Verb_When_Dispatching_Command_Line_Then_The_Handler_Is_Called()
    {
        var environment = ImportKworbChartCommandLineTestEnvironment.Create();

        await environment.Subject.DispatchAsync(["import-kworb-chart"], CancellationToken.None);

        environment.Handler.Calls.Should().Be(1);
    }
}
