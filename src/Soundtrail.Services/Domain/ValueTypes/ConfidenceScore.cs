namespace Soundtrail.Services.Domain.ValueTypes;

public readonly record struct ConfidenceScore
{
    private ConfidenceScore(double value)
    {
        Value = value;
    }

    public double Value { get; }

    public static ConfidenceScore From(double value)
    {
        if (value is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Confidence must be between 0 and 1.");
        }

        return new ConfidenceScore(value);
    }

    public override string ToString() => Value.ToString("0.00");
}
