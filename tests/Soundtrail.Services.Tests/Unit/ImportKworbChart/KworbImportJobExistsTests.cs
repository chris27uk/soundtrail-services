using TickerQ.Utilities.Base;

namespace Soundtrail.Services.Tests.Unit.ImportKworbChart;

public sealed class KworbImportJobExistsTests
{
    [Fact]
    public async Task Given_The_Kworb_Import_Job_When_Executing_Then_The_Import_Handler_Is_Called()
    {
        var environment = KworbImportJobUnitTestEnvironment.Create();

        await environment.CreateSubjectUnderTest().ImportKworbChart(new TickerFunctionContext(), CancellationToken.None);

        environment.Handler.Calls.Should().Be(1);
    }
}
