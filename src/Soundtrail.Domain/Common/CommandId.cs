namespace Soundtrail.Domain.Common;

public readonly record struct CommandId(string Value)
{
    public static CommandId New() => new(Guid.NewGuid().ToString("N"));

    public static CommandId For(string value) => new(value);

    public static CommandId From(string value) => new(value);

    public override string ToString() => Value;
    
    public static implicit operator string(CommandId commandId) => commandId.Value;
    
    public static implicit operator CommandId(string commandId) => new(commandId);
}
