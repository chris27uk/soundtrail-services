namespace MusicResolver.Application.Ports;

public interface IClockPort
{
    DateTimeOffset UtcNow { get; }
}
