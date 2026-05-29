using Soundtrail.Services.Shared;

namespace Soundtrail.Services.EnrichmentWorker;

public sealed class SystemClockAdapter : IClockPort
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
