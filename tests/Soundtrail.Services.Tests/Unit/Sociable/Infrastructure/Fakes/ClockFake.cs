using Soundtrail.Adapters.Timing;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

internal sealed class ClockFake(DateTimeOffset utcNow) : IClockPort
{
    public DateTimeOffset UtcNow { get; set; } = utcNow;
}
