namespace Soundtrail.Services.Features.Tracks;

public sealed record InstallId
{
    private InstallId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static InstallId New() => new(Guid.NewGuid().ToString("N"));

    public override string ToString() => Value;
}
