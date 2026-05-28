using MusicResolver.Application.Ports;

namespace MusicResolver.Infrastructure.Time;

public sealed class SystemClock : IClockPort
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
