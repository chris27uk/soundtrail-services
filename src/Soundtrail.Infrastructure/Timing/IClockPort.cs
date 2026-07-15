namespace Soundtrail.Adapters.Timing
{
    public interface IClockPort
    {
        DateTimeOffset UtcNow { get; }
    }
}
