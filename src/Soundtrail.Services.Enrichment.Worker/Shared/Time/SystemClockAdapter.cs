using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Worker;

public sealed class SystemClockAdapter : IClockPort
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
