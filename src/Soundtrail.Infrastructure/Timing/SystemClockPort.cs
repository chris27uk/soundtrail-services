namespace Soundtrail.Adapters.Timing
{
    public class SystemClockPort : IClockPort
    {
        public DateTimeOffset UtcNow  => DateTimeOffset.UtcNow;
    }
}
