using Soundtrail.Adapters.Timing;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

internal sealed class ClockFake(DateTimeOffset utcNow) : IClockPort
{
    public ClockFake() : this(new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero))
    {
    }

    public DateTimeOffset UtcNow { get; set; } = utcNow;
}
