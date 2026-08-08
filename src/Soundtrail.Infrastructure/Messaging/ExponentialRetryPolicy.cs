namespace Soundtrail.Adapters.Messaging;

internal sealed class ExponentialRetryPolicy
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaxDelay = TimeSpan.FromMinutes(5);
    private const int MaxRetryCount = 5;

    public TimeSpan? GetDelay(int retryCount)
    {
        if (retryCount >= MaxRetryCount)
        {
            return null;
        }

        var exponent = Math.Min(retryCount, 16);
        var multiplier = 1 << exponent;
        var delay = TimeSpan.FromSeconds(InitialDelay.TotalSeconds * multiplier);
        return delay <= MaxDelay ? delay : MaxDelay;
    }
}
