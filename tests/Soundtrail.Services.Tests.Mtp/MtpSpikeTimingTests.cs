namespace Soundtrail.Services.Tests.Mtp;

/// <summary>
/// Cheap tests whose start order under MTP is written to the diagnostics log.
/// </summary>
public sealed class MtpSpikeTimingTests
{
    [Fact]
    public void Given_Mtp_Is_Running_When_A_Fast_Test_Starts_Then_It_Logs_Immediately()
    {
        MtpSpikeDiagnostics.RecordTestStart(nameof(Given_Mtp_Is_Running_When_A_Fast_Test_Starts_Then_It_Logs_Immediately));
    }
}
