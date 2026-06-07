namespace Soundtrail.Services.Api.Infrastructure.Time;

public interface IClockPort
{
    DateTimeOffset UtcNow { get; }
}
