using FluentAssertions;
using Soundtrail.Services.Api.Infrastructure.Time;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.Clock.UtcNow;

public sealed class UtcNowResponsesTests
{
    [Theory]
    [MemberData(nameof(AllModes))]
    public void Given_A_Clock_Port_When_Reading_UtcNow_Then_A_Utc_Timestamp_Is_Returned(ClockPortMode mode)
    {
        IClockPort clock = mode == ClockPortMode.InProcessFake
            ? new FakeClockPort(new DateTimeOffset(2026, 6, 12, 12, 0, 0, TimeSpan.Zero))
            : new SystemClock();

        clock.UtcNow.Offset.Should().Be(TimeSpan.Zero);
    }

    public static IEnumerable<object[]> AllModes()
    {
        yield return [ClockPortMode.InProcessFake];
        yield return [ClockPortMode.System];
    }

    public enum ClockPortMode
    {
        InProcessFake,
        System
    }

    private sealed class FakeClockPort(DateTimeOffset utcNow) : IClockPort
    {
        public DateTimeOffset UtcNow => utcNow;
    }
}
