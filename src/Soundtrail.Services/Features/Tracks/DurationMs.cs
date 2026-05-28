namespace Soundtrail.Services.Features.Tracks;

public readonly record struct DurationMs
{
    private DurationMs(int value)
    {
        Value = value;
    }

    public int Value { get; }

    public static DurationMs From(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Duration must be non-negative.");
        }

        return new DurationMs(value);
    }

    public override string ToString() => Value.ToString();
}
