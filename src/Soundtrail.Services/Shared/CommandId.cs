namespace Soundtrail.Services.Shared;

public readonly record struct CommandId(string Value)
{
    public static CommandId New() => new(Guid.NewGuid().ToString("N"));

    public static CommandId For(string value) => new(Uri.EscapeDataString(value));

    public static CommandId From(string value) => new(value);

    public override string ToString() => Value;
}
