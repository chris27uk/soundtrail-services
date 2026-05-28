namespace Soundtrail.Services.Application.Ports;

public interface IClockPort
{
    DateTimeOffset UtcNow { get; }
}
