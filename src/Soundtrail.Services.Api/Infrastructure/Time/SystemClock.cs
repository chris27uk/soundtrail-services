namespace Soundtrail.Services.Api.Infrastructure.Time;

public sealed class SystemClock : IClockPort
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
