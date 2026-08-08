using Soundtrail.Domain.Discovery;
using TickerQ.Utilities.Base;

namespace Soundtrail.Services.Tests.Unit.ImportKworbChart;

public sealed class KworbImportJobExistsTests
{
    [Fact]
    public async Task Given_The_Kworb_Import_Job_When_Executing_Then_A_Known_Music_Data_Request_Is_Published()
    {
        var environment = KworbImportJobUnitTestEnvironment.Create();

        await environment.CreateSubjectUnderTest().ImportKworbChart(new TickerFunctionContext(), CancellationToken.None);

        environment.CommandBus.SentMessages.Should().ContainSingle().Which.Should().BeOfType<RequestKnownMusicDataMessage>();
    }
}
