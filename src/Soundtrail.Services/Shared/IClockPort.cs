namespace Soundtrail.Services.Shared;

public interface IClockPort
{
    DateTimeOffset UtcNow { get; }
}
